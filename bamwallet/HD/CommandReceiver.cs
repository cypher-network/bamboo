// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using BAMWallet.Extensions;
using BAMWallet.Helper;
using BAMWallet.Model;
using BAMWallet.Rpc;
using BAMWallet.Services;
using Dawn;
using Libsecp256k1Zkp.Net;
using Microsoft.Extensions.Options;
using NBitcoin;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NBitcoin.Stealth;
using Transaction = BAMWallet.Model.Transaction;
using Util = BAMWallet.Helper.Util;
using Constants = BAMWallet.HD.Constant;

namespace BAMWallet.HD
{
    public class CommandReceiver : ICommandReceiver
    {
        #region: CLASS_INTERNALS
        private const string HdPath = Constants.HD_PATH;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagProvider;
        private readonly ILogger _logger;
        private readonly NBitcoin.Network _network;
        private readonly Client _client;
        private readonly NetworkSettings _networkSettings;
        private static int _commandExecutionCounter;
        /// <summary>
        ///
        /// </summary>
        /// <param name="keyPath"></param>
        /// <param name="secretKey"></param>
        /// <param name="chainCode"></param>
        /// <returns></returns>
        private KeySet CreateKeySet(KeyPath keyPath, byte[] secretKey, byte[] chainCode)
        {
            Guard.Argument(keyPath, nameof(keyPath)).NotNull();
            Guard.Argument(secretKey, nameof(secretKey)).NotNull().MaxCount(32);
            Guard.Argument(chainCode, nameof(chainCode)).NotNull().MaxCount(32);

            var masterKey = new ExtKey(new Key(secretKey), chainCode);
            var spend = masterKey.Derive(keyPath).PrivateKey;
            var scan = masterKey.Derive(keyPath = keyPath.Increment()).PrivateKey;

            return new KeySet
            {
                ChainCode = masterKey.ChainCode.ByteToHex(),
                KeyPath = keyPath.ToString(),
                RootKey = masterKey.PrivateKey.ToHex(),
                StealthAddress = spend.PubKey.CreateStealthAddress(scan.PubKey, _network).ToString()
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private SecureString NewId(int bytes = 32)
        {
            using var secp256K1 = new Secp256k1();

            var secureString = new SecureString();
            foreach (var c in $"id_{secp256K1.RandomSeed(bytes).ByteToHex()}") secureString.AppendChar(c);

            return secureString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private TaskResult<bool> CalculateChange(Session session, WalletTransaction transaction)
        {
            try
            {
                var freeBalances = new List<Balance>();
                var (_, scan) = Unlock(session);
                var balances = AddBalances(session);

                if (transaction.Payment == 0)
                {
                    return TaskResult<bool>.CreateFailure(new Exception("Unable to use zero value payment."));
                }

                freeBalances.AddRange(balances
                    .Where(balance =>
                        !balance.Commitment.IsLockedOrInvalid() &&
                        transaction.Payment <= balance.Total).OrderByDescending(x => x.Total));

                if (!freeBalances.Any())
                {
                    return TaskResult<bool>.CreateFailure(new Exception("No free commitments available. Please retry after commitments unlock."));
                }

                var spending = freeBalances.First(x => x.Total >= transaction.Payment);
                var total = Transaction.Amount(spending.Commitment, scan);
                if (transaction.Payment > total)
                {
                    return TaskResult<bool>.CreateFailure(new Exception("The payment exceeds the total commitment balance"));
                }

                var change = total - transaction.Payment;

                transaction.Balance = total;
                transaction.Change = change;
                transaction.DateTime = DateTime.UtcNow;
                transaction.Id = session.SessionId;
                transaction.Spending = spending.Commitment;
                transaction.Spent = change == 0;
                transaction.Reward = session.SessionType == SessionType.Coinstake ? transaction.Reward : 0;

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error calculating change");
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private List<Balance> AddBalances(Session session)
        {
            var balances = new List<Balance>();
            try
            {
                var (_, scan) = Unlock(session);
                var walletTransactions = session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToList();
                if (walletTransactions?.Any() != true)
                {
                    return Enumerable.Empty<Balance>().ToList();
                }

                balances.AddRange(from balanceSheet in walletTransactions
                                  from output in balanceSheet.Transaction.Vout
                                  let keyImage = GetKeyImage(session, output)
                                  where keyImage != null
                                  let spent = WalletTransactionSpent(session, keyImage)
                                  where !spent
                                  let amount = Transaction.Amount(output, scan)
                                  where amount != 0
                                  select new Balance { Commitment = output, Total = Transaction.Amount(output, scan) });
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error adding balances");
            }

            return balances.DistinctBy(x => x.Total).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="transaction"></param>
        /// <param name="m"></param>
        /// <param name="nCols"></param>
        /// <param name="pcmOut"></param>
        /// <param name="blinds"></param>
        /// <param name="preimage"></param>
        /// <param name="pc"></param>
        /// <param name="ki"></param>
        /// <param name="ss"></param>
        /// <param name="bp"></param>
        /// <param name="offsets"></param>
        /// <returns></returns>
        private TaskResult<bool> GenerateTransaction(Session session, ref WalletTransaction transaction, byte[] m, int nCols, Span<byte[]> pcmOut,
            Span<byte[]> blinds, byte[] preimage, byte[] pc, byte[] ki, byte[] ss, byte[] bp, byte[] offsets)
        {
            try
            {
                var (outPkPayment, stealthPayment) = StealthPayment(transaction.RecipientAddress);
                var (outPkChange, stealthChange) = StealthPayment(transaction.SenderAddress);
                var tx = new Transaction
                {
                    Bp = new[] { new Bp { Proof = bp } },
                    Mix = nCols,
                    Rct = new[] { new RCT { I = preimage, M = m, P = pc, S = ss } },
                    Ver = 0x2,
                    Vin = new[] { new Vin { Key = new KeyOffsetImage { KImage = ki, KOffsets = offsets } } },
                    Vout = new[]
                    {
                        new Vout
                        {
                            A = session.SessionType == SessionType.Coinstake ? transaction.Payment : 0,
                            C = pcmOut[0],
                            D = session.SessionType == SessionType.Coinstake ? blinds[1] : Array.Empty<byte>(),
                            E = stealthPayment.Metadata.EphemKey.ToBytes(),
                            N = ScanPublicKey(transaction.RecipientAddress).Encrypt(
                                Transaction.Message(transaction.Payment, 0, blinds[1],
                                    transaction.Memo)),
                            P = outPkPayment.ToBytes(),
                            T = session.SessionType == SessionType.Coin ? CoinType.Payment : CoinType.Coinstake
                        },
                        new Vout
                        {
                            A = 0,
                            C = pcmOut[1],
                            E = stealthChange.Metadata.EphemKey.ToBytes(),
                            N = ScanPublicKey(transaction.SenderAddress).Encrypt(
                                Transaction.Message(transaction.Change, transaction.Payment,
                                    blinds[2], transaction.Memo)),
                            P = outPkChange.ToBytes(),
                            T = CoinType.Change
                        }
                    },
                    Id = session.SessionId
                };

                if (session.SessionType == SessionType.Coin)
                {
                    var generateTransactionTime = GenerateTransactionTime(tx.ToHash(), ref transaction);
                    if (!generateTransactionTime.Success)
                    {
                        return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                        {
                            success = false,
                            message = "Unable to generate the transaction delayed time"
                        }));
                    }

                    tx.Vtime = generateTransactionTime.Result;
                }
                else if (session.SessionType == SessionType.Coinstake)
                {
                    using var secp256K1 = new Secp256k1();
                    using var pedersen = new Pedersen();

                    var (outPkReward, stealthReward) = StealthPayment(transaction.SenderAddress);
                    var rewardLockTime = new LockTime(Util.DateTimeToUnixTime(DateTimeOffset.UtcNow.AddHours(21)));
                    var blind = pedersen.BlindSwitch(transaction.Reward, secp256K1.CreatePrivateKey());
                    var commit = pedersen.Commit(transaction.Reward, blind);
                    var vOutput = tx.Vout.ToList();
                    vOutput.Insert(0,
                        new Vout
                        {
                            A = transaction.Reward,
                            C = commit,
                            D = blind,
                            E = stealthReward.Metadata.EphemKey.ToBytes(),
                            L = rewardLockTime.Value,
                            N = ScanPublicKey(transaction.SenderAddress).Encrypt(Transaction.Message(
                                transaction.Reward, 0, blind, transaction.Memo)),
                            P = outPkReward.ToBytes(),
                            S = new Script(Op.GetPushOp(rewardLockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY)
                                .ToString(),
                            T = CoinType.Coinbase
                        });

                    tx.Vout = vOutput.ToArray();
                }
                tx.TxnId = tx.ToHash();
                transaction.Transaction = tx;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return TaskResult<bool>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="walletTransaction"></param>
        /// <returns></returns>
        private TaskResult<Vtime> GenerateTransactionTime(in byte[] hash, ref WalletTransaction walletTransaction)
        {
            Vtime vtime;

            try
            {
                var x = System.Numerics.BigInteger.Parse(hash.ByteToHex(),
                    System.Globalization.NumberStyles.AllowHexSpecifier);
                if (x.Sign <= 0)
                {
                    x = -x;
                }

                var timer = new Stopwatch();
                var t = (int)(walletTransaction.Delay * 3.2 * 1000);
                timer.Start();
                var nonce = Cryptography.Sloth.Eval(t, x);
                timer.Stop();
                var y = System.Numerics.BigInteger.Parse(nonce);
                var success = Cryptography.Sloth.Verify(t, x, y);
                if (!success)
                {
                    {
                        return TaskResult<Vtime>.CreateFailure(JObject.FromObject(new
                        {
                            success = false,
                            message = "Unable to verify the verified delayed function"
                        }));
                    }
                }

                if (timer.Elapsed.Seconds < 5)
                {
                    return TaskResult<Vtime>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Verified delayed function elapsed seconds is lower the than the default amount"
                    }));
                }

                var lockTime = Util.GetAdjustedTimeAsUnixTimestamp() & ~timer.Elapsed.Seconds;
                vtime = new Vtime
                {
                    I = t,
                    M = hash,
                    N = nonce.ToBytes(),
                    W = timer.Elapsed.Ticks,
                    L = lockTime,
                    S = new Script(Op.GetPushOp(lockTime), OpcodeType.OP_CHECKLOCKTIMEVERIFY).ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return TaskResult<Vtime>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<Vtime>.CreateSuccess(vtime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="spending"></param>
        /// <param name="blinds"></param>
        /// <param name="sk"></param>
        /// <param name="nRows"></param>
        /// <param name="nCols"></param>s
        /// <param name="index"></param>
        /// <param name="m"></param>
        /// <param name="pcmIn"></param>
        /// <param name="pkIn"></param>
        /// <returns></returns>
        private unsafe byte[] M(Session session, Vout spending, Span<byte[]> blinds, Span<byte[]> sk, int nRows,
            int nCols, int index, byte[] m, Span<byte[]> pcmIn, Span<byte[]> pkIn)
        {
            using var pedersen = new Pedersen();
            var (spend, scan) = Unlock(session);
            var transactions = SafeguardService.GetTransactions().ToArray();
        begin:
            transactions.Shuffle();
            for (var k = 0; k < nRows - 1; ++k)
                for (var i = 0; i < nCols; ++i)
                {
                    if (i == index)
                    {
                        try
                        {
                            var message = Transaction.Message(spending, scan);
                            var oneTimeSpendKey = spend.Uncover(scan, new PubKey(spending.E));
                            sk[0] = oneTimeSpendKey.ToHex().HexToByte();
                            blinds[0] = message.Blind;
                            pcmIn[i + k * nCols] = pedersen.Commit(message.Amount, message.Blind);
                            pkIn[i + k * nCols] = oneTimeSpendKey.PubKey.ToBytes();
                            fixed (byte* mm = m, pk = pkIn[i + k * nCols])
                            {
                                Libsecp256k1Zkp.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
                            }

                            continue;
                        }
                        catch (Exception ex)
                        {
                            _logger.Here().Error("Unable to create main ring member");
                            throw new Exception(ex.StackTrace);
                        }
                    }

                    try
                    {
                        var isLocked = transactions[i].IsLockedOrInvalid();
                        if (isLocked) goto begin;
                    }
                    catch (Exception)
                    {
                        _logger.Here().Error("Unable to check if locked or invalid");
                        goto begin;
                    }

                    try
                    {
                        pcmIn[i + k * nCols] = transactions[i].Vout[0].C;
                        pkIn[i + k * nCols] = transactions[i].Vout[0].P;
                        fixed (byte* mm = m, pk = pkIn[i + k * nCols])
                        {
                            Libsecp256k1Zkp.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Here().Error("Unable to create ring member");
                        throw new Exception(ex.StackTrace);
                    }
                }

            return m;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="keySet"></param>
        /// <returns></returns>
        private static ExtKey MasterKey(KeySet keySet)
        {
            return new(new Key(keySet.RootKey.HexToByte()), keySet.ChainCode.HexToByte());
        }

        /// <summary>
        /// Bulletproof commitment.
        /// </summary>
        /// <param name="balance"></param>
        /// <param name="blindSum"></param>
        /// <param name="commitSum"></param>
        /// <returns></returns>
        private static TaskResult<ProofStruct> BulletProof(ulong balance, byte[] blindSum, byte[] commitSum)
        {
            ProofStruct proofStruct;
            try
            {
                using var bulletProof = new BulletProof();
                using var sec256K1 = new Secp256k1();
                proofStruct = bulletProof.GenProof(balance, blindSum, sec256K1.RandomSeed(32), null!, null!, null!);
                var success = bulletProof.Verify(commitSum, proofStruct.proof, null!);
                if (!success)
                {
                    return TaskResult<ProofStruct>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Bulletproof Verify failed."
                    }));
                }
            }
            catch (Exception ex)
            {
                return TaskResult<ProofStruct>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<ProofStruct>.CreateSuccess(proofStruct);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pcin"></param>
        /// <param name="nCols"></param>
        /// <returns></returns>
        private static byte[] Offsets(Span<byte[]> pcin, int nCols)
        {
            var i = 0;
            const int k = 0;
            var offsets = new byte[nCols * 33];
            var pcmin = pcin.GetEnumerator();
            while (pcmin.MoveNext())
            {
                Buffer.BlockCopy(pcmin.Current, 0, offsets, (i + k * nCols) * 33, 33);
                i++;
            }

            return offsets;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="passphrase"></param>
        /// <param name="hdRoot"></param>
        private static void CreateHdRootKey(SecureString seed, SecureString passphrase, out ExtKey hdRoot)
        {
            Guard.Argument(seed, nameof(seed)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();
            var concatenateMnemonic = string.Join(" ", seed.ToUnSecureString());
            hdRoot = new Mnemonic(concatenateMnemonic).DeriveExtKey(passphrase.ToUnSecureString());
            concatenateMnemonic.ZeroString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="memo"></param>
        /// <param name="sent"></param>
        /// <param name="received"></param>
        /// <param name="reward"></param>
        /// <param name="balance"></param>
        /// <param name="outputs"></param>
        /// <param name="txId"></param>
        /// <param name="isVerified"></param>
        /// <param name="isLocked"></param>
        /// <returns></returns>
        private static BalanceSheet MoneyBalanceSheet(DateTime dateTime, string memo, ulong sent, ulong received,
            ulong reward, ulong balance, Vout[] outputs, string txId, bool isVerified, bool? isLocked = null)
        {
            var balanceSheet = new BalanceSheet
            {
                Date = dateTime.ToString("yyyy-MM-dd HH:mm"),
                Memo = memo,
                Balance = balance.DivWithGYin().ToString("F9"),
                Outputs = outputs,
                TxId = txId,
                IsVerified = isVerified,
                IsLocked = isLocked
            };
            if (sent != 0)
            {
                balanceSheet.MoneyOut = $"-{sent.DivWithGYin():F9}";
            }

            if (received != 0)
            {
                balanceSheet.MoneyIn = $"{received.DivWithGYin():F9}";
            }

            if (reward != 0)
            {
                balanceSheet.Reward = $"{reward.DivWithGYin():F9}";
            }

            return balanceSheet;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private (PubKey, StealthPayment) StealthPayment(string address)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty().NotWhiteSpace();
            var ephem = new Key();
            var stealth = new BitcoinStealthAddress(address, _network);
            var payment = stealth.CreatePayment(ephem);
            var outPk = stealth.SpendPubKeys[0].UncoverSender(ephem, stealth.ScanPubKey);
            return (outPk, payment);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private PubKey ScanPublicKey(string address)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty().NotWhiteSpace();
            var stealth = new BitcoinStealthAddress(address, _network);
            return stealth.ScanPubKey;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        private byte[] GetKeyImage(Session session, Vout output)
        {
            Guard.Argument(output, nameof(output)).NotNull();
            var (spend, scan) = Unlock(session);
            var oneTimeSpendKey = spend.Uncover(scan, new PubKey(output.E));
            var mlsag = new MLSAG();
            return mlsag.ToKeyImage(oneTimeSpendKey.ToHex().HexToByte(), oneTimeSpendKey.PubKey.ToBytes());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="transactions"></param>
        private void SyncTransactions(Session session, IEnumerable<WalletTransaction> transactions)
        {
            var walletTransactions = transactions.ToList();
            foreach (var transaction in walletTransactions.Select(walletTransaction => walletTransaction.Transaction))
            {
                if (TransactionDoesNotExist(transaction)) continue;
                var rolledBack = RollBackTransaction(session, transaction.Id);
                if (!rolledBack.Success)
                {
                    _logger.Here().Error(rolledBack.Exception.Message);
                    // Continue syncing rest of the wallet
                }
            }

            foreach (var transaction in walletTransactions.Where(walletTransaction => !walletTransaction.IsVerified))
            {
                if (!TransactionDoesNotExist(transaction.Transaction)) continue;
                transaction.IsVerified = true;
                var saved = Update(session, transaction);
                if (!saved.Result)
                {
                    _logger.Error("Transaction is verified but cannot update transaction {@TxId}",
                        transaction.Transaction.TxnId.HexToByte());
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool TransactionDoesNotExist(Transaction transaction)
        {
            return DoesTransactionExistInEndpoint(transaction.TxnId, MessageCommand.GetTransaction) ||
                   DoesTransactionExistInEndpoint(transaction.TxnId, MessageCommand.GetMemTransaction) ||
                   DoesTransactionExistInEndpoint(transaction.TxnId, MessageCommand.GetPosTransaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool DoesTransactionExistInEndpoint(byte[] transactionId, MessageCommand command)
        {
            try
            {
                if (command == MessageCommand.GetTransaction)
                {
                    var transactionResponse =
                        _client.Send<TransactionResponse>(command, new Parameter { Value = transactionId });
                    return transactionResponse.Transaction is { };
                }

                if (command == MessageCommand.GetMemTransaction)
                {
                    var memoryPoolTransactionResponse =
                        _client.Send<MemoryPoolTransactionResponse>(command, new Parameter { Value = transactionId });
                    return memoryPoolTransactionResponse.Transaction is { };
                }

                if (command == MessageCommand.GetPosTransaction)
                {
                    var posTransactionResponse =
                        _client.Send<PosPoolTransactionResponse>(command, new Parameter { Value = transactionId });
                    return posTransactionResponse.Transaction is { };
                }
            }
            catch (Exception)
            {
                _logger.Here().Warning("Connection to the node cannot be established");
            }
            // If we get this far then the connection was dropped
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private (Key, Key) Unlock(Session session)
        {
            Key spend = null;
            Key scan = null;
            try
            {
                var keySet = session.KeySet;
                var masterKey = MasterKey(keySet);
                spend = masterKey.Derive(new KeyPath($"{HdPath}0")).PrivateKey;
                scan = masterKey.Derive(new KeyPath($"{HdPath}1")).PrivateKey;
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error unlocking");
            }

            return (spend, scan);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data"></param>
        /// <param name="updateGuid"></param>
        /// <returns></returns>
        private TaskResult<bool> Save<T>(Session session, T data, bool updateGuid = true)
        {
            Guard.Argument(data, nameof(data)).NotEqual(default);
            try
            {
                session.Database.Insert(data);
                if (updateGuid)
                {
                    session.SessionId = Guid.NewGuid();
                }
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error saving");
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private TaskResult<bool> Update<T>(Session session, T data)
        {
            Guard.Argument(data, nameof(data)).NotEqual(default);
            try
            {
                session.Database.Update(data);
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error updating");
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private TaskResult<bool> RollBackTransaction(in Session session, Guid id)
        {
            Guard.Argument(id, nameof(id)).NotDefault();
            try
            {
                var walletTransaction = session.Database.Query<WalletTransaction>()
                    .Where(s => s.Id == id).FirstOrDefault();
                if (walletTransaction != null)
                {
                    session.Database.Delete<WalletTransaction>(new LiteDB.BsonValue(walletTransaction.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error rolling back transaction");
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="paymentId"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private bool AlreadyReceivedPayment(string paymentId, in Session session)
        {
            var walletTransactions = session.Database.Query<WalletTransaction>().ToList();
            return walletTransactions.FirstOrDefault(x => x.Transaction.TxnId.Xor(paymentId.HexToByte())) != null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        private bool WalletTransactionSpent(Session session, byte[] image)
        {
            Guard.Argument(image, nameof(image)).NotNull().MaxCount(33);
            var spent = false;
            var walletTransactions = session.Database.Query<WalletTransaction>().ToList();
            var transactions = walletTransactions.Where(x => x.Transaction.Vin.Any(t => t.Key.KImage.Xor(image)));
            try
            {
                spent = transactions.Any();
            }
            catch (Exception)
            {
                // Ignore
            }

            return spent;
        }

        #endregion

        #region: PUBLIC_API

        #region: NON_DB_RELATED_FUNCTIONS

        public CommandReceiver(IHostApplicationLifetime lifetime, ISafeguardDownloadingFlagProvider safeguardDownloadingFlagProvider,
            IOptions<NetworkSettings> networkSettings, ILogger logger)
        {
            _applicationLifetime = lifetime;
            _safeguardDownloadingFlagProvider = safeguardDownloadingFlagProvider;
            _networkSettings = networkSettings.Value;
            _network = _networkSettings.Environment == Constant.Mainnet ? NBitcoin.Network.Main : NBitcoin.Network.TestNet;
            _logger = logger.ForContext("SourceContext", nameof(CommandReceiver));
            _client = new Client(networkSettings.Value, _logger);
            _commandExecutionCounter = 0;
        }

        /// <summary>
        /// BIP39 seed.
        /// </summary>
        /// <returns></returns>
        public string[] CreateSeed(in WordCount wordCount)
        {
            var task = Task.Run(async () => await Wordlist.LoadWordList(Language.English));
            task.Wait();
            var mnemonic = new Mnemonic(task.Result, wordCount);
            return mnemonic.Words;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public Tuple<object, string> WalletList()
        {
            var baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            try
            {
                if (Directory.Exists(baseDir))
                {
                    var walletsDir = Path.Combine(baseDir, Constants.WALLET_DIR_SUFFIX);
                    if (Directory.Exists(walletsDir))
                    {
                        var files = Directory.GetFiles(walletsDir, Constants.WALLET_FILE_EXTENSION).ToList();
                        if (files.Count != 0)
                        {
                            files.Sort();
                            return new Tuple<object, string>(files, string.Empty);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return new Tuple<object, string>(null, "No wallets found!");
        }

        /// <summary>
        /// 
        /// </summary>
        public static void IncrementCommandExecutionCount()
        {
            ++_commandExecutionCounter;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void DecrementCommandExecutionCount()
        {
            --_commandExecutionCounter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="passphrase"></param>
        /// <param name="walletName"></param>
        /// <returns></returns>
        public Task<string> CreateWallet(in SecureString seed, in SecureString passphrase, in string walletName)
        {
            using var commandExecutionGuard =
                new RAIIGuard(IncrementCommandExecutionCount, DecrementCommandExecutionCount);
            Guard.Argument(seed, nameof(seed)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();
            seed.MakeReadOnly();
            passphrase.MakeReadOnly();
            try
            {
                CreateHdRootKey(seed, passphrase, out var hdRoot);
                var keySet = CreateKeySet(new KeyPath($"{HdPath}0"), hdRoot.PrivateKey.ToHex().HexToByte(),
                    hdRoot.ChainCode);
                var db = Util.LiteRepositoryFactory(walletName, passphrase);
                db.Insert(keySet);
                keySet.ChainCode.ZeroString();
                keySet.RootKey.ZeroString();
                return Task.FromResult(walletName);
            }
            catch (Exception)
            {
                //_logger.Here().Error(ex, "Error creating wallet");
                throw new Exception("Failed to create wallet.");
            }
            finally
            {
                seed.Dispose();
                passphrase.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Tuple<object, string> Address(in Session session)
        {
            using var commandExecutionGuard = new RAIIGuard(CommandReceiver.IncrementCommandExecutionCount,
                CommandReceiver.DecrementCommandExecutionCount);
            string address = null;
            try
            {
                address = session.KeySet.StealthAddress;
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting address");
                return new Tuple<object, string>(null, ex.Message);
            }

            return new Tuple<object, string>(address, String.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Tuple<object, string> CreateTransaction(Session session, ref WalletTransaction transaction)
        {
            using var commandExecutionGuard = new RAIIGuard(CommandReceiver.IncrementCommandExecutionCount,
                CommandReceiver.DecrementCommandExecutionCount);
            Guard.Argument(session.SessionId, nameof(session.SessionId)).NotDefault();
            while (_safeguardDownloadingFlagProvider.TryDownloading)
            {
                Thread.Sleep(100);
            }

            var calculated = CalculateChange(session, transaction);
            if (!calculated.Success)
            {
                return new Tuple<object, string>(null, calculated.Exception.Message);
            }

            using var secp256K1 = new Secp256k1();
            using var pedersen = new Pedersen();
            using var mlsag = new MLSAG();
            var blinds = new Span<byte[]>(new byte[3][]);
            var sk = new Span<byte[]>(new byte[2][]);
            const int nRows = 2; // last row sums commitments
            const int nCols = 22; // ring size
            var index = Libsecp256k1Zkp.Net.Util.Rand(0, nCols) % nCols;
            var m = new byte[nRows * nCols * 33];
            var pcmIn = new Span<byte[]>(new byte[nCols * 1][]);
            var pcmOut = new Span<byte[]>(new byte[2][]);
            var randSeed = secp256K1.Randomize32();
            var preimage = secp256K1.Randomize32();
            var pc = new byte[32];
            var ki = new byte[33 * 1];
            var ss = new byte[nCols * nRows * 32];
            var blindSum = new byte[32];
            var pkIn = new Span<byte[]>(new byte[nCols * 1][]);
            m = M(session, transaction.Spending, blinds, sk, nRows, nCols, index, m, pcmIn, pkIn);
            var payment = transaction.Payment;
            var change = transaction.Change;
            blinds[1] = pedersen.BlindSwitch(payment, secp256K1.CreatePrivateKey());
            blinds[2] = pedersen.BlindSwitch(change, secp256K1.CreatePrivateKey());
            pcmOut[0] = pedersen.Commit(payment, blinds[1]);
            pcmOut[1] = pedersen.Commit(change, blinds[2]);
            var commitSumBalance = pedersen.CommitSum(new List<byte[]> { pcmOut[0], pcmOut[1] }, new List<byte[]>());
            if (!pedersen.VerifyCommitSum(new List<byte[]> { commitSumBalance }, new List<byte[]> { pcmOut[0], pcmOut[1] }))
            {
                return new Tuple<object, string>(null, "Verify commit sum failed.");
            }

            var bulletChange = BulletProof(change, blinds[2], pcmOut[1]);
            if (!bulletChange.Success)
            {
                return new Tuple<object, string>(null, bulletChange.Exception.Message);
            }

            var success = mlsag.Prepare(m, blindSum, pcmOut.Length, pcmOut.Length, nCols, nRows, pcmIn, pcmOut, blinds);
            if (!success)
            {
                return new Tuple<object, string>(null, "MLSAG Prepare failed.");
            }

            sk[nRows - 1] = blindSum;
            success = mlsag.Generate(ki, pc, ss, randSeed, preimage, nCols, nRows, index, sk, m);
            if (!success)
            {
                return new Tuple<object, string>(null, "MLSAG Generate failed.");
            }

            success = mlsag.Verify(preimage, nCols, nRows, m, ki, pc, ss);
            if (!success)
            {
                return new Tuple<object, string>(null, "MLSAG Verify failed.");
            }

            var offsets = Offsets(pcmIn, nCols);
            var generateTransaction = GenerateTransaction(session, ref transaction, m, nCols, pcmOut, blinds, preimage,
                pc, ki, ss, bulletChange.Result.proof, offsets);
            if (!generateTransaction.Success)
            {
                return new Tuple<object, string>(null,
                    $"Unable to make the transaction. Inner error message {generateTransaction.NonSuccessMessage.message}");
            }

            var saved = Save(session, transaction, false);
            return !saved.Success
                ? new Tuple<object, string>(null, "Unable to save the transaction.")
                : new Tuple<object, string>(transaction, String.Empty);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Tuple<object, string> History(in Session session)
        {
            using var commandExecutionGuard =
                new RAIIGuard(IncrementCommandExecutionCount, DecrementCommandExecutionCount);
            var balanceSheets = new List<BalanceSheet>();
            var walletTransactions = session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToList();
            if (walletTransactions?.Any() != true)
            {
                return new Tuple<object, string>(null, "Unable to find any wallet transactions");
            }

            try
            {
                var (_, scan) = Unlock(session);
                ulong received = 0;
                foreach (var transaction in walletTransactions.Select(x => x.Transaction))
                {
                    var isLocked = transaction.IsLockedOrInvalid();
                    var walletTransaction = walletTransactions.First(x => x.Transaction.TxnId.Xor(transaction.TxnId));
                    var payment = transaction.Vout.Where(z => z.T is CoinType.Payment).ToArray();
                    if (payment.Any())
                    {
                        try
                        {
                            var messagePayment = Transaction.Message(payment.ElementAt(0), scan);
                            if (messagePayment != null)
                            {
                                if (messagePayment.Amount != 0)
                                {
                                    received += messagePayment.Amount;
                                    balanceSheets.Add(MoneyBalanceSheet(messagePayment.Date, messagePayment.Memo, 0,
                                        messagePayment.Amount, 0, received, payment, transaction.TxnId.ByteToHex(),
                                        walletTransaction.IsVerified, isLocked));
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }

                    var change = transaction.Vout
                        .Where(z => z.T is CoinType.Change or CoinType.Coinstake or CoinType.Coinbase).ToArray();
                    if (!change.Any()) continue;
                    try
                    {
                        if (change.ElementAt(0).T == CoinType.Coinbase)
                        {
                            var messageCoinbase = Transaction.Message(change.ElementAt(0), scan);
                            var messageCoinstake = Transaction.Message(change.ElementAt(1), scan);
                            received -= messageCoinstake.Amount;
                            balanceSheets.Add(MoneyBalanceSheet(messageCoinstake.Date, messageCoinstake.Memo,
                                messageCoinstake.Amount, 0, messageCoinbase.Amount, received, change,
                                transaction.TxnId.ByteToHex(), walletTransaction.IsVerified, isLocked));
                            continue;
                        }

                        var messageChange = Transaction.Message(change.ElementAt(0), scan);
                        received -= messageChange.Paid;
                        balanceSheets.Add(MoneyBalanceSheet(messageChange.Date, messageChange.Memo, messageChange.Paid,
                            0, 0, received, change, transaction.TxnId.ByteToHex(), walletTransaction.IsVerified,
                            isLocked));
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting history");
                return new Tuple<object, string>(null, ex.Message);
            }

            return new Tuple<object, string>(balanceSheets.OrderBy(x => x.Date), String.Empty);
        }

        // Function locks entire wallet instead of acting on the commitment level.
        public bool IsTransactionAllowed(in Session session)
        {
            //SyncWallet(session);
            //if there are still non verified but in mempool transactions return false, else we're good to go
            //bool doesUnverifiedTransactionExist = session.Database.Query<WalletTransaction>().Where(x => !x.IsVerified)
            //.ToList().Any(); 
            //return (doesUnverifiedTransactionExist == false);
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public Tuple<object, string> ReceivePayment(in Session session, string paymentId)
        {
            using var commandExecutionGuard = new RAIIGuard(CommandReceiver.IncrementCommandExecutionCount,
                CommandReceiver.DecrementCommandExecutionCount);
            Guard.Argument(paymentId, nameof(paymentId)).NotNull().NotEmpty().NotWhiteSpace();
            try
            {
                if (AlreadyReceivedPayment(paymentId, session))
                {
                    return new Tuple<object, string>(null, $"Transaction with paymentId: {paymentId} already exists");
                }

                var transactionResponse = _client.Send<TransactionResponse>(MessageCommand.GetTransaction,
                    new Parameter { Value = paymentId.HexToByte() });
                if (transactionResponse is null)
                {
                    return new Tuple<object, string>(null, $"Failed to find transaction with paymentId: {paymentId}");
                }

                var (spend, scan) = Unlock(session);
                var outputs = (from v in transactionResponse.Transaction.Vout
                               let uncover = spend.Uncover(scan, new PubKey(v.E))
                               where uncover.PubKey.ToBytes().SequenceEqual(v.P)
                               select v.Cast<Vout>()).ToList();
                if (false == outputs.Any())
                {
                    return new Tuple<object, string>(null, "Your stealth address does not control this payment");
                }

                var tx = new WalletTransaction
                {
                    SenderAddress = session.KeySet.StealthAddress,
                    DateTime = DateTime.UtcNow,
                    Transaction = new Transaction
                    {
                        Bp = transactionResponse.Transaction.Bp,
                        Mix = transactionResponse.Transaction.Mix,
                        Rct = transactionResponse.Transaction.Rct,
                        TxnId = transactionResponse.Transaction.TxnId,
                        Vtime = transactionResponse.Transaction.Vtime,
                        Vout = outputs.ToArray(),
                        Vin = transactionResponse.Transaction.Vin,
                        Ver = transactionResponse.Transaction.Ver
                    },
                    WalletType = WalletType.Receive,
                    IsVerified = true
                };
                var saved = Save(session, tx);
                return saved.Success
                    ? new Tuple<object, string>(tx, string.Empty)
                    : new Tuple<object, string>(null, saved.Exception.Message);
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error receiving payment");
                var message = ex.Message;
                return new Tuple<object, string>(null, $"{message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        public Tuple<object, string> SendTransaction(in Session session, ref WalletTransaction tx)
        {
            using var commandExecutionGuard =
                new RAIIGuard(IncrementCommandExecutionCount, DecrementCommandExecutionCount);
            try
            {
                _client.HasRemoteAddress();
                var blockCountResponse = _client.Send<BlockCountResponse>(new Parameter
                {
                    MessageCommand = MessageCommand.GetBlockCount
                });
                if (blockCountResponse.Count != 0)
                {
                    var transaction = tx;
                    var walletTransaction = session.Database.Query<WalletTransaction>()
                        .Where(x => x.Transaction.Id == transaction.Id).FirstOrDefault();
                    if (walletTransaction is not null)
                    {
                        walletTransaction.BlockHeight = (ulong)blockCountResponse.Count;
                        Update(session, walletTransaction);
                    }
                }

                var newTransactionResponse = _client.Send<NewTransactionResponse>(new Parameter
                {
                    Value = tx.Transaction.Serialize(), MessageCommand = MessageCommand.Transaction
                });
                if (newTransactionResponse.Ok)
                {
                    return new Tuple<object, string>(true, string.Empty);
                }

                RollBackTransaction(session, tx.Transaction.Id);
                return new Tuple<object, string>(false,
                    $"Unable to send transaction with paymentId: {tx.Transaction.TxnId.ByteToHex()}");
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error sending transaction");
                var message = ex.Message;
                return new Tuple<object, string>(false, $"{message}");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        public void SyncWallet(in Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            var walletTransactionsNotVerified = session.Database.Query<WalletTransaction>().Where(x => !x.IsVerified).OrderBy(d => d.DateTime).ToList();
            var walletTransactionsTakeLast2 = session.Database.Query<WalletTransaction>().OrderBy(d => d.DateTime).ToList().TakeLast(2);
            var walletTransactions = walletTransactionsTakeLast2 as WalletTransaction[] ?? walletTransactionsTakeLast2.ToArray();
            walletTransactions.ToList().AddRange(walletTransactionsNotVerified);
            SyncTransactions(session, walletTransactions);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public Tuple<object, string> RecoverTransactions(in Session session, int start)
        {
            using var commandExecutionGuard = new RAIIGuard(CommandReceiver.IncrementCommandExecutionCount,
                CommandReceiver.DecrementCommandExecutionCount);
            Guard.Argument(start, nameof(start)).NotNegative();
            try
            {
                _client.HasRemoteAddress();
                var walletTransactions = session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToList();
                if (walletTransactions.Count > 0)
                {
                    var transactionBlockIndexResponse = _client.Send<TransactionBlockIndexResponse>(
                        MessageCommand.GetTransactionBlockIndex,
                        new Parameter {Value = walletTransactions.Last().Transaction.TxnId});
                    if (transactionBlockIndexResponse is { })
                    {
                        start = (int) transactionBlockIndexResponse.Index;
                    }
                }

                if (start == 0)
                {
                    var wExists = session.Database.Query<WalletTransaction>().Exists();
                    if (wExists)
                    {
                        var dropped = session.Database.Database.DropCollection($"{nameof(WalletTransaction)}");
                        if (!dropped)
                        {
                            var message = $"Unable to drop collection for {nameof(WalletTransaction)}";
                            _logger.Here().Error(message);
                            return new Tuple<object, string>(null, message);
                        }
                    }
                }

                var blockCountResponse = _client.Send<BlockCountResponse>(MessageCommand.GetBlockCount);
                if (blockCountResponse is null) return new Tuple<object, string>(null, "Error recovering block count");
                var height = (int) blockCountResponse.Count;
                const int maxBlocks = 10;
                var chunks = Enumerable.Repeat(maxBlocks, height / maxBlocks).ToList();
                if (height % maxBlocks != 0) chunks.Add(height % maxBlocks);
                foreach (var chunk in chunks)
                {
                    var blocksResponse = _client.Send<BlocksResponse>(MessageCommand.GetBlocks,
                        new Parameter {Value = start.ToByte()}, new Parameter {Value = chunk.ToByte()});
                    if (blocksResponse.Blocks is { })
                    {
                        foreach (var transaction in blocksResponse.Blocks.SelectMany(x => x.Txs))
                        {
                            var (spend, scan) = Unlock(session);
                            var outputs = (from v in transaction.Vout
                                let uncover = spend.Uncover(scan, new PubKey(v.E))
                                where uncover.PubKey.ToBytes().Xor(v.P)
                                select v.Cast<Vout>()).ToList();
                            if (outputs.Any() != true) continue;
                            if (AlreadyReceivedPayment(transaction.TxnId.ByteToHex(), session)) continue;
                            var tx = new WalletTransaction
                            {
                                Id = session.SessionId,
                                SenderAddress = session.KeySet.StealthAddress,
                                DateTime = DateTime.UtcNow,
                                Transaction = new Transaction
                                {
                                    Id = session.SessionId,
                                    Bp = transaction.Bp,
                                    Mix = transaction.Mix,
                                    Rct = transaction.Rct,
                                    TxnId = transaction.TxnId,
                                    Vtime = transaction.Vtime,
                                    Vout = outputs.ToArray(),
                                    Vin = transaction.Vin,
                                    Ver = transaction.Ver
                                },
                                WalletType = WalletType.Restore,
                                Delay = 5,
                                IsVerified = true
                            };
                            var saved = Save(session, tx);
                            if (!saved.Success)
                            {
                                _logger.Here().Error("Unable to save transaction: {@Transaction}",
                                    transaction.TxnId.ByteToHex());
                            }
                        }
                    }
                    else
                    {
                        break;
                    }

                    start += chunk;
                }

                return new Tuple<object, string>(true, null);
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error recovering transactions");
                return new Tuple<object, string>(null, $"Error recovering transactions: {ex.Message}");
            }
        }

        #endregion
    }
}