// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using BAMWallet.Extensions;
using BAMWallet.Helper;
using BAMWallet.Model;
using BAMWallet.Rpc;
using BAMWallet.Services;
using Dawn;
using Libsecp256k1Zkp.Net;
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
using MessagePack;
using NBitcoin.DataEncoders;
using NBitcoin.Stealth;
using Transaction = BAMWallet.Model.Transaction;
using Util = BAMWallet.Helper.Util;
using Constants = BAMWallet.HD.Constant;

namespace BAMWallet.HD
{
    public class CommandReceiver : ICommandReceiver
    {
        public CommandReceiver(ISafeguardDownloadingFlagProvider safeguardDownloadingFlagProvider, ILogger logger)
        {
            _safeguardDownloadingFlagProvider = safeguardDownloadingFlagProvider;
            _networkSettings = Util.LiteRepositoryAppSettingsFactory().Query<NetworkSettings>().First();
            _network = _networkSettings.Environment == Constant.Mainnet
                ? NBitcoin.Network.Main
                : NBitcoin.Network.TestNet;
            _logger = logger.ForContext("SourceContext", nameof(CommandReceiver));
            _client = new Client(_logger);
            _commandExecutionCounter = 0;
        }

        #region: CLASS_INTERNALS

        private const string HdPath = Constants.HD_PATH;
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
        /// <param name="session"></param>
        /// <param name="walletTransaction"></param>
        /// <returns></returns>
        private TaskResult<bool> CalculateChange(Session session, WalletTransaction walletTransaction)
        {
            try
            {
                var balances = GetBalances(session);
                if (walletTransaction.Payment == 0)
                    return TaskResult<bool>.CreateFailure(new Exception("Unable to use zero value payment amount."));
                var freeBalances = new List<Balance>();
                freeBalances.AddRange(balances.Where(balance => !balance.Commitment.IsLockedOrInvalid())
                    .OrderByDescending(x => x.Total));
                if (!freeBalances.Any())
                    return TaskResult<bool>.CreateFailure(
                        new Exception("No free commitments available. Please retry after commitments unlock."));
                var payment = walletTransaction.Payment;
                const ulong max = 18446740000;
                var totals = new List<Balance>();
                while (payment != 0 || freeBalances.Count != 0)
                {
                    var spend = freeBalances.Select(x => x.Total)
                        .Aggregate((x, y) => x - payment < y - payment ? x : y);
                    var bal = freeBalances.First(a => a.Total == spend);
                    freeBalances.Remove(bal);
                    totals.Add(bal);
                    payment -= spend;
                    if (payment is >= max or <= 0) break;
                }

                if (totals.Count == 0)
                    return TaskResult<bool>.CreateFailure(new Exception("No free commitments available."));
                var total = totals.Sum(x => x.Total.DivWithGYin());
                if (walletTransaction.Payment > total.ConvertToUInt64())
                    return TaskResult<bool>.CreateFailure(
                        new Exception("The payment exceeds the total commitment balance."));
                walletTransaction.DateTime = DateTime.UtcNow;
                walletTransaction.Id = session.SessionId;
                walletTransaction.Spending = totals.Select(x => x.Commitment).ToArray();
                walletTransaction.SpendAmounts = totals.Select(x => x.Total).ToArray();
                walletTransaction.Spent = total.ConvertToUInt64() - walletTransaction.Payment == 0;
                walletTransaction.Reward = session.SessionType == SessionType.Coinstake ? walletTransaction.Reward : 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error calculating change.");
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Balance[] GetBalances(Session session)
        {
            var balances = new List<Balance>();
            try
            {
                var (_, scan) = Unlock(session);
                foreach (var walletTransaction in session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToArray())
                {
                    if (walletTransaction.State is not (WalletTransactionState.Confirmed
                        or WalletTransactionState.NotFound)) continue;
                    foreach (var output in walletTransaction.Transaction.Vout)
                    {
                        if (walletTransaction.State == WalletTransactionState.Confirmed)
                        {
                            var keyImage = GetKeyImage(session, output);
                            if (keyImage == null) continue;
                            var spent = IsSpent(session, keyImage);
                            if (spent) continue;
                            balances.Add(new Balance
                            {
                                DateTime = walletTransaction.DateTime,
                                Commitment = output,
                                State = walletTransaction.State,
                                Total = Transaction.Amount(output, scan),
                                TxnId = walletTransaction.Transaction.TxnId
                            });
                        }
                        else
                        {
                            var message = Transaction.Message(output, scan);
                            if (message == null) continue;
                            balances.Add(new Balance
                            {
                                DateTime = walletTransaction.DateTime,
                                Commitment = output,
                                State = walletTransaction.State,
                                Total = message.Paid,
                                TxnId = walletTransaction.Transaction.TxnId
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error adding balances.");
            }

            return balances.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="walletTransaction"></param>
        /// <param name="ringCt"></param>
        /// <returns></returns>
        private TaskResult<bool> GenerateTransaction(Session session, ref WalletTransaction walletTransaction,
            ref RingConfidentialTransaction ringCt)
        {
            try
            {
                var (outPkPayment, stealthPayment) = StealthPayment(walletTransaction.RecipientAddress);
                var (outPkChange, stealthChange) = StealthPayment(walletTransaction.SenderAddress);
                var tx = new Transaction
                {
                    Bp = new[] { new Bp { Proof = ringCt.Bp } },
                    Mix = ringCt.Cols,
                    Rct = new[] { new RCT { I = ringCt.Preimage, M = ringCt.M, P = ringCt.Pc, S = ringCt.Ss } },
                    Vin = new[] { new Vin { Key = new KeyOffsetImage { KImage = ringCt.Ki, KOffsets = ringCt.Offsets } } },
                    Vout = new[]
                    {
                        new Vout
                        {
                            A =
                                session.SessionType == SessionType.Coinstake
                                    ? walletTransaction.Payment
                                    : 0,
                            C = ringCt.PcmOut[0],
                            D = session.SessionType == SessionType.Coinstake
                                ? ringCt.Blinds[1]
                                : Array.Empty<byte>(),
                            E = stealthPayment.Metadata.EphemKey.ToBytes(),
                            N = ScanPublicKey(walletTransaction.RecipientAddress).Encrypt(
                                Transaction.Message(walletTransaction.Payment, 0, ringCt.Blinds[1],
                                    walletTransaction.Memo)),
                            P = outPkPayment.ToBytes(),
                            T = session.SessionType == SessionType.Coin
                                ? CoinType.Payment
                                : CoinType.Coinstake
                        },
                        new Vout
                        {
                            A = 0,
                            C = ringCt.PcmOut[1],
                            E = stealthChange.Metadata.EphemKey.ToBytes(),
                            N = ScanPublicKey(walletTransaction.SenderAddress).Encrypt(
                                Transaction.Message(walletTransaction.Change, walletTransaction.Payment,
                                    ringCt.Blinds[2], walletTransaction.Memo)),
                            P = outPkChange.ToBytes(),
                            T = CoinType.Change
                        }
                    },
                    Id = session.SessionId
                };

                if (session.SessionType == SessionType.Coinstake)
                {
                    using var secp256K1 = new Secp256k1();
                    using var pedersen = new Pedersen();
                    var (outPkReward, stealthReward) = StealthPayment(walletTransaction.SenderAddress);
                    var rewardLockTime = new LockTime(Util.DateTimeToUnixTime(DateTimeOffset.UtcNow.AddHours(21)));
                    var blind = pedersen.BlindSwitch(walletTransaction.Reward, secp256K1.CreatePrivateKey());
                    var commit = pedersen.Commit(walletTransaction.Reward, blind);
                    var vOutput = tx.Vout.ToList();
                    vOutput.Insert(0,
                        new Vout
                        {
                            A = walletTransaction.Reward,
                            C = commit,
                            D = blind,
                            E = stealthReward.Metadata.EphemKey.ToBytes(),
                            L = rewardLockTime.Value,
                            N = ScanPublicKey(walletTransaction.SenderAddress).Encrypt(Transaction.Message(
                                walletTransaction.Reward, 0, blind, walletTransaction.Memo)),
                            P = outPkReward.ToBytes(),
                            S = new Script(Op.GetPushOp(rewardLockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY)
                                .ToString(),
                            T = CoinType.Coinbase
                        });
                    tx.Vout = vOutput.ToArray();
                }

                walletTransaction.Transaction = tx;
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
        /// <param name="transaction"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        private TaskResult<Vtime> GenerateTransactionTime(in byte[] hash, ref Transaction transaction, in int delay)
        {
            Vtime vTime;

            try
            {
                var x = System.Numerics.BigInteger.Parse(hash.ByteToHex(),
                    System.Globalization.NumberStyles.AllowHexSpecifier);
                if (x.Sign <= 0)
                {
                    x = -x;
                }

                var size = transaction.GetSize() / 1024;
                var timer = new Stopwatch();
                var t = (int)(delay * decimal.Round(size, 2, MidpointRounding.ToZero) * 600 * (decimal)1.6);
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
                            message = "Unable to verify the verified delayed function."
                        }));
                    }
                }

                if (timer.Elapsed.Seconds < 5)
                {
                    return TaskResult<Vtime>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Verified delayed function elapsed seconds is lower the than the default amount."
                    }));
                }

                var lockTime = Util.GetAdjustedTimeAsUnixTimestamp() & ~timer.Elapsed.Seconds;
                vTime = new Vtime
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

            return TaskResult<Vtime>.CreateSuccess(vTime);
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
        private unsafe byte[] RingMembers(Session session, Vout spending, Span<byte[]> blinds, Span<byte[]> sk,
            int nRows,
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
                            _logger.Here().Error("Unable to create main ring member.");
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
                        _logger.Here().Error("Unable to check if locked or invalid.");
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
                        _logger.Here().Error("Unable to create ring member.");
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
            return new ExtKey(new Key(keySet.RootKey.HexToByte()), keySet.ChainCode.HexToByte());
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
            var concatenateMnemonic = string.Join(" ", seed.FromSecureString());
            hdRoot = new Mnemonic(concatenateMnemonic).DeriveExtKey(passphrase.FromSecureString());
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
        /// <param name="state"></param>
        /// <param name="isLocked"></param>
        /// <returns></returns>
        private static BalanceSheet MoneyBalanceSheet(DateTime dateTime, string memo, ulong sent, ulong received,
            ulong reward, ulong balance, Vout[] outputs, string txId, WalletTransactionState state,
            bool? isLocked = null)
        {
            var balanceSheet = new BalanceSheet
            {
                Date = dateTime.ToString("dd-MM-yyyy HH:mm:ss"),
                Memo = memo,
                Balance = balance.DivWithGYin().ToString("F9"),
                Outputs = outputs,
                TxId = txId,
                State = state,
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
        private void SyncTransactions(in Session session, IEnumerable<WalletTransaction> transactions)
        {
            var walletTransactions = transactions.ToList();
            foreach (var walletTransaction in walletTransactions.Where(walletTransaction =>
                         walletTransaction.State == WalletTransactionState.WaitingConfirmation))
            {
                var state = DoesTransactionExistInEndpoint(walletTransaction.Transaction.TxnId,
                    walletTransaction.BlockHeight);
                if (state is WalletTransactionState.Syncing) continue;
                walletTransaction.State = state switch
                {
                    WalletTransactionState.WaitingConfirmation => WalletTransactionState.WaitingConfirmation,
                    WalletTransactionState.NotFound => WalletTransactionState.NotFound,
                    WalletTransactionState.Confirmed => WalletTransactionState.Confirmed,
                    _ => walletTransaction.State
                };
                var saved = Update(session, walletTransaction);
                if (!saved.Result)
                {
                    _logger.Error("Transaction state is {@state} but cannot update transaction {@TxId}", state,
                        walletTransaction.Transaction.TxnId.HexToByte());
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        private WalletTransactionState DoesTransactionExistInEndpoint(byte[] transactionId, ulong blockHeight)
        {
            try
            {
                var blockCountResponse = _client.Send<BlockCountResponse>(new Parameter
                {
                    MessageCommand = MessageCommand.GetBlockCount
                });
                if (blockCountResponse.Count != 0)
                {
                    if ((ulong)blockCountResponse.Count > blockHeight)
                    {
                        var transactionResponse = _client.Send<TransactionBlockIndexResponse>(new Parameter
                        {
                            Value = transactionId,
                            MessageCommand = MessageCommand.GetTransactionBlockIndex
                        });
                        if (transactionResponse.Index == 0)
                        {
                            return WalletTransactionState.NotFound;
                        }

                        if ((ulong)blockCountResponse.Count >=
                            transactionResponse.Index + _networkSettings.NumberOfConfirmations)
                        {
                            return WalletTransactionState.Confirmed;
                        }
                    }
                    else
                    {
                        return WalletTransactionState.WaitingConfirmation;
                    }
                }
            }
            catch (Exception)
            {
                _logger.Here().Warning("Connection to the node cannot be established!");
            }

            // If we get this far then the connection was dropped
            return WalletTransactionState.Syncing;
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
                _logger.Here().Error(ex, "Error unlocking.");
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
                _logger.Here().Error(ex, "Error saving.");
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
                _logger.Here().Error(ex, "Error updating.");
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
                _logger.Here().Error(ex, "Error rolling back transaction.");
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
        private static bool AlreadyReceivedPayment(string paymentId, in Session session)
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
        private static bool IsSpent(Session session, byte[] image)
        {
            Guard.Argument(image, nameof(image)).NotNull().MaxCount(33);
            var spent = false;
            try
            {
                var walletTransactions = session.Database.Query<WalletTransaction>().ToList();
                foreach (var _ in from walletTransaction in walletTransactions from vin in walletTransaction.Transaction.Vin where vin.Key.KImage.Xor(image) select vin)
                {
                    spent = true;
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return spent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <exception cref="Exception"></exception>
        private static Transaction[] ReadWalletTransactions(in Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            var walletTransactions = session.Database.Query<WalletTransaction>().ToList();
            return walletTransactions.Select(x => x.Transaction).ToArray();
        }

        #endregion

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Tuple<object, string> History(in Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            using var commandExecutionGuard =
                new RAIIGuard(IncrementCommandExecutionCount, DecrementCommandExecutionCount);
            var balanceSheets = new List<BalanceSheet>();
            try
            {
                var walletTransactions = session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToList();
                if (walletTransactions?.Any() != true)
                {
                    return new Tuple<object, string>(null, "Unable to find any wallet transactions.");
                }

                var (_, scan) = Unlock(session);
                ulong received = 0;
                foreach (var transaction in walletTransactions.Where(n => n.State != WalletTransactionState.NotFound)
                             .Select(x => x.Transaction))
                {
                    var isLocked = transaction.IsLockedOrInvalid();
                    var walletTransaction = walletTransactions.First(x => x.Transaction.TxnId.Xor(transaction.TxnId));
                    foreach (var payment in transaction.Vout.Where(z => z.T is CoinType.Payment))
                    {
                        var messagePayment = Transaction.Message(payment, scan);
                        if (messagePayment == null) continue;
                        if (messagePayment.Amount == 0) continue;
                        received += messagePayment.Amount;
                        balanceSheets.Add(MoneyBalanceSheet(messagePayment.Date, messagePayment.Memo, 0,
                            messagePayment.Amount, 0, received, new[] { payment }, transaction.TxnId.ByteToHex(),
                            walletTransaction.State, isLocked));
                    }

                    var change = transaction.Vout
                        .Where(z => z.T is CoinType.Change or CoinType.Coinstake or CoinType.Coinbase).ToArray();
                    if (!change.Any()) continue;
                    if (change.ElementAt(0).T == CoinType.Coinbase)
                    {
                        var messageCoinbase = Transaction.Message(change.ElementAt(0), scan);
                        var messageCoinstake = Transaction.Message(change.ElementAt(1), scan);
                        received -= messageCoinstake.Amount;
                        received += messageCoinstake.Amount;
                        received += messageCoinbase.Amount;
                        balanceSheets.Add(MoneyBalanceSheet(messageCoinstake.Date, messageCoinstake.Memo,
                            messageCoinstake.Amount, messageCoinstake.Amount, messageCoinbase.Amount, received, change,
                            transaction.TxnId.ByteToHex(), walletTransaction.State, isLocked));
                        continue;
                    }

                    foreach (var paid in change)
                    {
                        var messageChange = Transaction.Message(paid, scan);
                        received -= messageChange.Paid;
                        balanceSheets.Add(MoneyBalanceSheet(messageChange.Date, messageChange.Memo, messageChange.Paid,
                            0, 0, received, new[] { paid }, transaction.TxnId.ByteToHex(), walletTransaction.State,
                            isLocked));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting history.");
                return new Tuple<object, string>(null, ex.Message);
            }

            return new Tuple<object, string>(balanceSheets, string.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Tuple<object, string> NotFoundTransactions(in Session session)
        {
            var balanceSheets = new List<BalanceSheet>();
            try
            {
                var walletTransactions = session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToList();
                if (walletTransactions?.Any() != true)
                {
                    return new Tuple<object, string>(null, "Unable to find any wallet transactions.");
                }

                var (_, scan) = Unlock(session);
                foreach (var transaction in walletTransactions.Where(n => n.State == WalletTransactionState.NotFound)
                             .Select(x => x.Transaction))
                {
                    var walletTransaction = walletTransactions.First(x => x.Transaction.TxnId.Xor(transaction.TxnId));
                    var payment = transaction.Vout.First(z => z.T is CoinType.Payment);
                    try
                    {
                        var messagePayment = Transaction.Message(payment, scan);
                        if (messagePayment != null)
                        {
                            if (messagePayment.Amount != 0)
                            {
                                balanceSheets.Add(MoneyBalanceSheet(messagePayment.Date, messagePayment.Memo, 0,
                                    messagePayment.Amount, 0, 0, new[] { payment }, transaction.TxnId.ByteToHex(),
                                    walletTransaction.State, false));
                            }
                        }

                        var change = transaction.Vout.First(z => z.T is CoinType.Change);
                        var messageChange = Transaction.Message(change, scan);
                        if (messageChange != null)
                        {
                            balanceSheets.Add(MoneyBalanceSheet(messageChange.Date, messageChange.Memo,
                                messageChange.Paid,
                                0, 0, messageChange.Amount, new[] { change }, transaction.TxnId.ByteToHex(),
                                walletTransaction.State,
                                false));
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting history.");
                return new Tuple<object, string>(null, ex.Message);
            }

            return new Tuple<object, string>(balanceSheets, string.Empty);
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
            using var commandExecutionGuard = new RAIIGuard(IncrementCommandExecutionCount,
                DecrementCommandExecutionCount);
            string address;
            try
            {
                address = session.KeySet.StealthAddress;
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting address.");
                return new Tuple<object, string>(null, ex.Message);
            }

            return new Tuple<object, string>(address, string.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="addressBook"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public Tuple<object, string> AddAddressBook(Session session, ref AddressBook addressBook, bool update = false)
        {
            using var commandExecutionGuard = new RAIIGuard(IncrementCommandExecutionCount,
                DecrementCommandExecutionCount);
            Guard.Argument(session.SessionId, nameof(session.SessionId)).NotDefault();
            Guard.Argument(addressBook, nameof(addressBook)).NotNull();
            if (!IsBase58(addressBook.RecipientAddress))
            {
                return new Tuple<object, string>(null, "Recipient address does not phrase to a base58 format.");
            }

            var addressBooks = session.Database.Query<AddressBook>().OrderBy(x => x.Created).ToList();
            if (addressBooks.Any())
            {
                var book = addressBook;
                var findAddressBook = addressBooks.FirstOrDefault(x => x.RecipientAddress == book.RecipientAddress || x.Name == book.Name);
                if (findAddressBook != null && !update)
                {
                    return new Tuple<object, string>(null,
                        $"Recipient name: {addressBook.Name} with address: {addressBook.RecipientAddress} already exists.");
                }
            }

            var saved = update ? Update(session, addressBook) : Save(session, addressBook);
            return saved.Success
                ? new Tuple<object, string>(addressBook, string.Empty)
                : new Tuple<object, string>(null, saved.Exception.Message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="addressBook"></param>
        /// <returns></returns>
        public Tuple<object, string> FindAddressBook(Session session, ref AddressBook addressBook)
        {
            using var commandExecutionGuard = new RAIIGuard(IncrementCommandExecutionCount,
                DecrementCommandExecutionCount);
            Guard.Argument(session.SessionId, nameof(session.SessionId)).NotDefault();
            Guard.Argument(addressBook, nameof(addressBook)).NotNull();
            var addressBooks = session.Database.Query<AddressBook>().OrderBy(x => x.Created).ToList();
            if (!addressBooks.Any())
            {
                return new Tuple<object, string>(null,
                    $"Recipient {addressBook.Name} does not exists.");
            }

            var book = addressBook;
            var findAddressBook = addressBooks.FirstOrDefault(x => x.RecipientAddress == book.RecipientAddress || x.Name == book.Name);
            return findAddressBook != null
                ? new Tuple<object, string>(findAddressBook, string.Empty)
                : new Tuple<object, string>(null,
                    $"Recipient not found.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="addressBook"></param>
        /// <returns></returns>
        public Tuple<object, string> RemoveAddressBook(Session session, ref AddressBook addressBook)
        {
            using var commandExecutionGuard = new RAIIGuard(IncrementCommandExecutionCount,
                DecrementCommandExecutionCount);
            Guard.Argument(session.SessionId, nameof(session.SessionId)).NotDefault();
            Guard.Argument(addressBook, nameof(addressBook)).NotNull();
            var addressBooks = session.Database.Query<AddressBook>().OrderBy(x => x.Created).ToList();
            if (!addressBooks.Any())
            {
                return new Tuple<object, string>(null,
                    $"Recipient name: {addressBook.Name} with address: {addressBook.RecipientAddress} already exists.");
            }

            var book = addressBook;
            var findAddressBook =
                addressBooks.FirstOrDefault(x => x.RecipientAddress == book.RecipientAddress || x.Name == book.Name);
            if (findAddressBook is null)
                return new Tuple<object, string>(null, $"Address book not found for {addressBook.Name}");
            var deleted = session.Database.Delete<AddressBook>(findAddressBook.Id);
            return deleted
                ? new Tuple<object, string>(findAddressBook, $"Address book deleted for {findAddressBook.Name}")
                : new Tuple<object, string>(null, $"Unable to delete address book for {addressBook.Name}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Tuple<object, string> ListAddressBook(Session session)
        {
            using var commandExecutionGuard = new RAIIGuard(IncrementCommandExecutionCount,
                DecrementCommandExecutionCount);
            Guard.Argument(session.SessionId, nameof(session.SessionId)).NotDefault();
            var addressBooks = session.Database.Query<AddressBook>().OrderBy(x => x.Created).ToList();
            if (!addressBooks.Any())
            {
                return new Tuple<object, string>(null,
                    "No recipients found!");
            }

            return new Tuple<object, string>(addressBooks.ToArray(), string.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="walletTransaction"></param>
        /// <returns></returns>
        public Tuple<object, string> CreateTransaction(Session session, ref WalletTransaction walletTransaction)
        {
            using var commandExecutionGuard = new RAIIGuard(IncrementCommandExecutionCount,
                DecrementCommandExecutionCount);
            Guard.Argument(session.SessionId, nameof(session.SessionId)).NotDefault();
            while (_safeguardDownloadingFlagProvider.TryDownloading)
            {
                Thread.Sleep(100);
            }

            if (!IsBase58(walletTransaction.RecipientAddress))
            {
                return new Tuple<object, string>(null, "Recipient address does not phrase to a base58 format.");
            }

            var calculated = CalculateChange(session, walletTransaction);
            if (!calculated.Success) return new Tuple<object, string>(null, calculated.Exception.Message);
            var (_, scan) = Unlock(session);
            var transactions = new List<Transaction>();
            var payment = walletTransaction.Payment;
            foreach (var spendAmount in walletTransaction.SpendAmounts)
            {
                var output = walletTransaction.Spending.First(x => Transaction.Amount(x, scan) >= spendAmount);
                var outputAmount = Transaction.Amount(output, scan);
                var amount = outputAmount;
                var isChange = outputAmount % payment;
                var change = (ulong)Math.Abs((long)(payment - outputAmount));
                payment = change;
                if (isChange != change)
                {
                    amount = outputAmount - payment;
                    change = payment;
                }
                else
                {
                    change = 0;
                }

                var ringConfidentialTransaction = RingCT(session, amount, change, output);
                var newWalletTransaction = new WalletTransaction
                {
                    Balance = amount,
                    Change = change,
                    Id = session.SessionId,
                    Memo = walletTransaction.Memo,
                    Payment = amount,
                    Reward = session.SessionType == SessionType.Coinstake ? walletTransaction.Reward : 0,
                    Spent = change == 0,
                    DateTime = DateTime.UtcNow,
                    RecipientAddress = walletTransaction.RecipientAddress,
                    SenderAddress = walletTransaction.SenderAddress,
                    WalletType = WalletType.Send
                };
                var generateTransaction =
                    GenerateTransaction(session, ref newWalletTransaction, ref ringConfidentialTransaction);
                if (!generateTransaction.Success)
                {
                    return new Tuple<object, string>(null,
                        $"Unable to generate the transaction. Inner error message {generateTransaction.NonSuccessMessage.message}");
                }

                transactions.Add(newWalletTransaction.Transaction);
            }

            var tx = new Transaction
            {
                Bp = transactions.SelectMany(x => x.Bp).ToArray(),
                Id = session.SessionId,
                Rct = transactions.SelectMany(x => x.Rct).ToArray(),
                Vin = transactions.SelectMany(x => x.Vin).ToArray(),
                Vout = transactions.SelectMany(x => x.Vout).ToArray()
            };

            if (session.SessionType == SessionType.Coin)
            {
                var generateTransactionTime = GenerateTransactionTime(tx.ToHash(), ref tx, walletTransaction.Delay);
                if (!generateTransactionTime.Success)
                {
                    return new Tuple<object, string>(null, "Unable to set the transaction priority time.");
                }

                tx.Vtime = generateTransactionTime.Result;
            }

            tx.TxnId = tx.ToHash();
            walletTransaction.Transaction = tx;
            walletTransaction.State = WalletTransactionState.WaitingConfirmation;
            var saved = Save(session, walletTransaction, false);
            return !saved.Success
                ? new Tuple<object, string>(null, "Unable to save the transaction.")
                : new Tuple<object, string>(walletTransaction, string.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="payment"></param>
        /// <param name="change"></param>
        /// <param name="commitment"></param>
        /// <returns></returns>
        private RingConfidentialTransaction RingCT(in Session session, ulong payment, ulong change, Vout commitment)
        {
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
            var ki = new byte[33];
            var ss = new byte[nCols * nRows * 32];
            var blindSum = new byte[32];
            var pkIn = new Span<byte[]>(new byte[nCols * 1][]);
            m = RingMembers(session, commitment, blinds, sk, nRows, nCols, index, m, pcmIn, pkIn);
            blinds[1] = pedersen.BlindSwitch(payment, secp256K1.CreatePrivateKey());
            blinds[2] = pedersen.BlindSwitch(change, secp256K1.CreatePrivateKey());
            pcmOut[0] = pedersen.Commit(payment, blinds[1]);
            pcmOut[1] = pedersen.Commit(change, blinds[2]);
            var commitSumBalance = pedersen.CommitSum(new List<byte[]> { pcmOut[0], pcmOut[1] }, new List<byte[]>());
            if (!pedersen.VerifyCommitSum(new List<byte[]> { commitSumBalance },
                    new List<byte[]> { pcmOut[0], pcmOut[1] }))
                throw new Exception("Verify commit sum failed.");

            var bulletChange = BulletProof(change, blinds[2], pcmOut[1]);
            if (!bulletChange.Success)
                throw new Exception(bulletChange.Exception.Message);

            var success = mlsag.Prepare(m, blindSum, pcmOut.Length, pcmOut.Length, nCols, nRows, pcmIn, pcmOut, blinds);
            if (!success)
                throw new Exception("MLSAG Prepare failed.");

            sk[nRows - 1] = blindSum;
            success = mlsag.Generate(ki, pc, ss, randSeed, preimage, nCols, nRows, index, sk, m);
            if (!success)
                throw new Exception("MLSAG Generate failed.");

            success = mlsag.Verify(preimage, nCols, nRows, m, ki, pc, ss);
            if (!success)
                throw new Exception("MLSAG Verify failed.");

            var offsets = Offsets(pcmIn, nCols);
            var ringCt = new RingConfidentialTransaction
            {
                Blinds = blinds,
                Bp = bulletChange.Result.proof,
                Ki = ki,
                M = m,
                Cols = nCols,
                Offsets = offsets,
                Pc = pc,
                Preimage = preimage,
                Ss = ss,
                PcmOut = pcmOut
            };

            return ringCt;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public Tuple<object, string> ReceivePayment(in Session session, string paymentId)
        {
            using var commandExecutionGuard = new RAIIGuard(IncrementCommandExecutionCount,
                DecrementCommandExecutionCount);
            Guard.Argument(paymentId, nameof(paymentId)).NotNull().NotEmpty().NotWhiteSpace();
            try
            {
                if (AlreadyReceivedPayment(paymentId, session))
                {
                    return new Tuple<object, string>(null, $"Transaction with paymentId: {paymentId} already exists.");
                }

                var transactionResponse = _client.Send<TransactionResponse>(
                    new Parameter { Value = paymentId.HexToByte(), MessageCommand = MessageCommand.GetTransaction });
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
                    return new Tuple<object, string>(null, "Your stealth address does not control this payment.");
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
                    State = WalletTransactionState.Confirmed,
                    Delay = 5
                };
                var saved = Save(session, tx);
                return saved.Success
                    ? new Tuple<object, string>(tx, string.Empty)
                    : new Tuple<object, string>(null, saved.Exception.Message);
            }
            catch (Exception)
            {
                return new Tuple<object, string>(null,
                    $"Unable to find transaction with paymentId: {paymentId}. It could be on its way. Please try again in a few seconds.");
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
                    Value = tx.Transaction.Serialize(),
                    MessageCommand = MessageCommand.Transaction
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
                _logger.Here().Error(ex, "Error sending transaction.");
                var message = ex.Message;
                return new Tuple<object, string>(false, $"{message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="stakeCredentialsRequest"></param>
        /// <param name="privateKey"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<MessageResponse<StakeCredentialsResponse>> SendStakeCredentials(in Session session,
            in StakeCredentialsRequest stakeCredentialsRequest, in byte[] privateKey, in byte[] token)
        {
            Guard.Argument(stakeCredentialsRequest, nameof(stakeCredentialsRequest)).NotNull();
            Guard.Argument(privateKey, nameof(privateKey)).NotNull().NotEmpty().MaxCount(32);
            Guard.Argument(token, nameof(token)).NotNull().NotEmpty().MaxCount(16);
            using var commandExecutionGuard =
                new RAIIGuard(IncrementCommandExecutionCount, DecrementCommandExecutionCount);
            if (!IsBase58(System.Text.Encoding.UTF8.GetString(stakeCredentialsRequest.RewardAddress)))
            {
                return Task.FromResult(new MessageResponse<StakeCredentialsResponse>(new StakeCredentialsResponse
                {
                    Success = false,
                    Message = "Reward address does not phrase to a base58 format."
                }));
            }

            var stakeCredentials = new StakeCredentialsRequest
            {
                Passphrase = stakeCredentialsRequest.Passphrase,
                RewardAddress = stakeCredentialsRequest.RewardAddress,
                Seed = stakeCredentialsRequest.Seed,
                Transactions = ReadWalletTransactions(in session)
            };
            var packet = Cryptography.Crypto.EncryptChaCha20Poly1305(MessagePackSerializer.Serialize(stakeCredentials),
                privateKey, token, out var tag, out var nonce);
            if (packet is null)
                return Task.FromResult(new MessageResponse<StakeCredentialsResponse>(
                    new StakeCredentialsResponse { Success = false, Message = "Failed to encrypt message." }));
            var stakeRequest = new StakeRequest { Tag = tag, Nonce = nonce, Data = packet, Token = token };
            var mStakeCredentialsResponse = _client.Send<StakeCredentialsResponse>(new Parameter
            {
                Value = MessagePackSerializer.Serialize(stakeRequest),
                MessageCommand = MessageCommand.Stake
            });
            return Task.FromResult(new MessageResponse<StakeCredentialsResponse>(new StakeCredentialsResponse
            {
                Success = mStakeCredentialsResponse.Success,
                Message = mStakeCredentialsResponse.Message
            }));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        public Task SyncWallet(in Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            var walletTransactionsNotVerified = session.Database.Query<WalletTransaction>()
                .Where(x => x.State == WalletTransactionState.WaitingConfirmation).OrderBy(d => d.DateTime).ToList();
            SyncTransactions(session, walletTransactionsNotVerified);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public ulong GetLastTransactionHeight(in Session session)
        {
            ulong start = 0;
            var walletTransactions = session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToList();
            if (walletTransactions.Count != 0)
            {
                start = walletTransactions.Last().BlockHeight;
            }

            return start;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="start"></param>
        /// <param name="recoverCompletely"></param>
        /// <returns></returns>
        public Tuple<object, string> RecoverTransactions(in Session session, int start, bool recoverCompletely = false)
        {
            using var commandExecutionGuard =
                new RAIIGuard(IncrementCommandExecutionCount, DecrementCommandExecutionCount);
            Guard.Argument(start, nameof(start)).NotNegative();
            try
            {
                if (!recoverCompletely)
                {
                    if (start == 0)
                    {
                        start = (int)GetLastTransactionHeight(session);
                    }
                }
                else
                {
                    using var db = Util.LiteRepositoryFactory(session.Identifier.FromSecureString(),
                        session.Passphrase);
                    var wExists = db.Query<WalletTransaction>().Exists();
                    if (wExists)
                    {
                        var dropped = db.Database.DropCollection($"{nameof(WalletTransaction)}");
                        if (!dropped)
                        {
                            var message = $"Unable to drop collection for {nameof(WalletTransaction)}";
                            _logger.Here().Error(message);
                            return new Tuple<object, string>(false, message);
                        }
                    }
                }

                _client.HasRemoteAddress();
                var blockCountResponse = _client.Send<BlockCountResponse>(new Parameter { MessageCommand = MessageCommand.GetBlockCount });
                if (blockCountResponse is null) return new Tuple<object, string>(null, "Error recovering block count.");
                var height = (int)blockCountResponse.Count;

                if (start > height)
                {
                    return new Tuple<object, string>(null, $"Please remove then restore the wallet! The ({_networkSettings.RemoteNode}) node block height: [{height}] " +
                                                           $"is smaller than the wallet start block height: [{start}]");
                }

                const int maxBlocks = 10;
                var chunks = Enumerable.Repeat(maxBlocks, height / maxBlocks).ToList();
                if (height % maxBlocks != 0) chunks.Add(height % maxBlocks);
                foreach (var chunk in chunks)
                {
                    var blocksResponse = _client.Send<BlocksResponse>(
                        new Parameter { Value = start.ToByte(), MessageCommand = MessageCommand.GetBlocks },
                        new Parameter { Value = chunk.ToByte(), MessageCommand = MessageCommand.GetBlocks });
                    if (blocksResponse.Blocks is { })
                    {
                        foreach (var block in blocksResponse.Blocks)
                        {
                            foreach (var transaction in block.Txs)
                            {
                                var (spend, scan) = Unlock(session);
                                var outputs = (from v in transaction.Vout
                                               let uncover = spend.Uncover(scan, new PubKey(v.E))
                                               where uncover.PubKey.ToBytes().Xor(v.P)
                                               select v).ToList();
                                if (outputs.Any() != true) continue;
                                if (AlreadyReceivedPayment(transaction.TxnId.ByteToHex(), session)) continue;
                                var tx = new WalletTransaction
                                {
                                    Id = session.SessionId,
                                    BlockHeight = block.Height,
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
                                    State = WalletTransactionState.Confirmed
                                };
                                var saved = Save(session, tx);
                                if (!saved.Success)
                                {
                                    _logger.Here().Error("Unable to save transaction: {@Transaction}",
                                        transaction.TxnId.ByteToHex());
                                }
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
                _logger.Here().Error(ex, "Error recovering transactions.");
                return new Tuple<object, string>(null, $"Error recovering transactions: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsBase58(string address)
        {
            var base58CheckEncoder = new Base58CheckEncoder();
            var isBase58 = base58CheckEncoder.IsMaybeEncoded(address);
            try
            {
                base58CheckEncoder.DecodeData(address);
            }
            catch (Exception)
            {
                isBase58 = false;
            }

            return isBase58;
        }
    }
}
