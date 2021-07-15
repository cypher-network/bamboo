// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using BAMWallet.Extensions;
using Newtonsoft.Json.Linq;
using Dawn;
using NBitcoin;
using NBitcoin.Stealth;
using Transaction = BAMWallet.Model.Transaction;
using Util = BAMWallet.Helper.Util;
using Block = BAMWallet.Model.Block;
using BAMWallet.Helper;
using BAMWallet.Model;
using BAMWallet.Rpc;
using BAMWallet.Services;
using Libsecp256k1Zkp.Net;
using Microsoft.Extensions.Options;
using Serilog;

namespace BAMWallet.HD
{
    public class WalletService : IWalletService
    {
        private static readonly AutoResetEvent _resetEvent = new(false);

        private const string HdPath = "m/44'/847177'/0'/0/";

        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagProvider;
        private readonly ILogger _logger;
        private readonly NBitcoin.Network _network;
        private readonly Client _client;
        private readonly NetworkSettings _networkSettings;
        private ConcurrentDictionary<Guid, Session> Sessions { get; }

        public WalletService(ISafeguardDownloadingFlagProvider safeguardDownloadingFlagProvider, IOptions<NetworkSettings> networkSettings, ILogger logger)
        {
            _safeguardDownloadingFlagProvider = safeguardDownloadingFlagProvider;
            _networkSettings = networkSettings.Value;
            _network = _networkSettings.Environment == Constant.Mainnet ? NBitcoin.Network.Main : NBitcoin.Network.TestNet;
            _logger = logger.ForContext("SourceContext", nameof(WalletService));
            _client = new Client(networkSettings.Value, _logger);

            Sessions = new ConcurrentDictionary<Guid, Session>();
        }

        public Client HttpClient() => _client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public Session Session(Guid sessionId) => Sessions.GetValueOrDefault(sessionId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Session SessionAddOrUpdate(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            var mSession = Sessions.AddOrUpdate(session.SessionId, session, (key, existingVal) =>
            {
                if (session != existingVal)
                    throw new ArgumentException("Duplicate sessions are not allowed: {0}.",
                        session.SessionId.ToString());

                try
                {
                    existingVal.WalletTransaction.Balance = session.WalletTransaction.Balance;
                    existingVal.WalletTransaction.Change = session.WalletTransaction.Change;
                    existingVal.WalletTransaction.DateTime = session.WalletTransaction.DateTime;
                    existingVal.WalletTransaction.Reward = session.WalletTransaction.Reward;
                    existingVal.WalletTransaction.Id = session.SessionId;
                    existingVal.WalletTransaction.Memo = session.WalletTransaction.Memo;
                    existingVal.WalletTransaction.Payment = session.WalletTransaction.Payment;
                    existingVal.WalletTransaction.RecipientAddress = session.WalletTransaction.RecipientAddress;
                    existingVal.WalletTransaction.SenderAddress = session.WalletTransaction.SenderAddress;
                    existingVal.WalletTransaction.Spent = session.WalletTransaction.Spent;
                    existingVal.WalletTransaction.Transaction = session.WalletTransaction.Transaction;
                    existingVal.WalletTransaction.WalletType = session.WalletTransaction.WalletType;
                }
                catch (Exception)
                {
                    // ignored
                }

                return existingVal;
            });

            return mSession;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        public void AddKeySet(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            try
            {
                var session = Session(sessionId);
                var next = LastKeySet(session.SessionId);
                var keyPath = new KeyPath(next.KeyPath);
                var index = keyPath.Indexes[3] + 1;
                var keySet = CreateKeySet(new KeyPath($"m/44'/847177'/{index}'/0/0"), next.RootKey.HexToByte(),
                    next.ChainCode.HexToByte());
                session.Database.Insert(keySet);
                next.ChainCode.ZeroString();
                next.RootKey.ZeroString();
                keySet.RootKey.ZeroString();
                keySet.ChainCode.ZeroString();
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error adding key set");
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyPath"></param>
        /// <param name="secretKey"></param>
        /// <param name="chainCode"></param>
        /// <returns></returns>
        public KeySet CreateKeySet(KeyPath keyPath, byte[] secretKey, byte[] chainCode)
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
        /// <param name="mnemonic"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public string CreateWallet(SecureString mnemonic, SecureString passphrase)
        {
            Guard.Argument(mnemonic, nameof(mnemonic)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            var walletId = NewId(16);

            walletId.MakeReadOnly();
            mnemonic.MakeReadOnly();
            passphrase.MakeReadOnly();

            CreateHdRootKey(mnemonic, passphrase, out var concatenateMnemonic, out var hdRoot);

            concatenateMnemonic.ZeroString();

            var keySet = CreateKeySet(new KeyPath($"{HdPath}0"), hdRoot.PrivateKey.ToHex().HexToByte(),
                hdRoot.ChainCode);

            try
            {
                var db = Util.LiteRepositoryFactory(walletId, passphrase);
                db.Insert(keySet);

                keySet.ChainCode.ZeroString();
                keySet.RootKey.ZeroString();

                return walletId.ToUnSecureString();
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error creating wallet");
                throw new Exception("Failed to create wallet.");
            }
            finally
            {
                walletId.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public SecureString NewId(int bytes = 32)
        {
            using var secp256K1 = new Secp256k1();

            var secureString = new SecureString();
            foreach (var c in $"id_{secp256K1.RandomSeed(bytes).ByteToHex()}") secureString.AppendChar(c);

            return secureString;
        }

        /// <summary>
        /// BIP39 mnemonic.
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> CreateMnemonic(Language language, WordCount wordCount)
        {
            var wordList = await Wordlist.LoadWordList(language);
            var mnemo = new Mnemonic(wordList, wordCount);

            return mnemo.Words;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public Transaction GetTransaction(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId).EnforceDbExists();
            var walletTransaction = session.Database.Query<WalletTransaction>().Where(x => x.Id == session.SessionId)
                .FirstOrDefault();

            return walletTransaction?.Transaction;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TaskResult<IEnumerable<string>> WalletList()
        {
            var wallets = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? string.Empty,
                "wallets");

            string[] files;
            try
            {
                files = Directory.GetFiles(wallets, "*.db");
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting wallet list");
                return TaskResult<IEnumerable<string>>.CreateFailure(ex);
            }

            return TaskResult<IEnumerable<string>>.CreateSuccess(files);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public IEnumerable<KeySet> KeySets(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId);

            var keys = session.Database.Query<KeySet>().ToList();
            return keys ?? Enumerable.Empty<KeySet>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<IEnumerable<string>> Addresses(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var addresses = Enumerable.Empty<string>();
            try
            {
                var session = Session(sessionId).EnforceDbExists();
                var keys = KeySets(session.SessionId);
                if (keys != null) addresses = keys.Select(k => k.StealthAddress);
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting addresses");
                return TaskResult<IEnumerable<string>>.CreateFailure(ex);
            }

            return TaskResult<IEnumerable<string>>.CreateSuccess(addresses);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<bool> CalculateChange(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
        
            try
            {
                var session = Session(sessionId);
                var (_, scan) = Unlock(sessionId);
                var multiBalance = new List<decimal>();
                var divisions = new List<Balance>();
                var divisionAmount = session.WalletTransaction.Payment.DivWithNaT();
                var amount = session.WalletTransaction.Payment.DivWithNaT();
                var balances = AddBalances(session.SessionId);
        
                foreach (var balance in balances.OrderByDescending(x => x.Total))
                {
                    var t = balance.Total.DivWithNaT();
                    var count = (int)(amount / t);
                    try
                    {
                        var g = t / divisionAmount;
                        if ((int)g != 0)
                        {
                            divisions.Add(balance);
                        }
                    }
                    catch (DivideByZeroException)
                    {
                        // Ignore
                    }
        
                    if (count != 0)
                        for (var k = 0; k < count; k++)
                            multiBalance.Add(balance.Total.DivWithNaT());
                    amount %= t;
                }
        
                var useAmount = divisions.Min(x => x.Total);
                if (useAmount == 0)
                {
                    return TaskResult<bool>.CreateFailure(new Exception("Multi single transactions are not implemented"));
                }
        
                var rem = useAmount.DivWithNaT();
                var closest = balances.Select(x => x.Total.DivWithNaT())
                    .Aggregate((x, y) => Math.Abs(x - rem) < Math.Abs(y - rem) ? x : y);
                var tx = balances.First(a => a.Total.DivWithNaT() == closest);
                var reward = session.SessionType == SessionType.Coinstake ? session.WalletTransaction.Reward : 0;
                var total = Transaction.Amount(tx.Commitment, scan);
                if (session.WalletTransaction.Payment > total)
                {
                    return TaskResult<bool>.CreateFailure(new Exception("The payment exceeds the total commitment balance"));
                }
                
                var change = total - session.WalletTransaction.Payment;

                session.WalletTransaction = new WalletTransaction
                {
                    Balance = total,
                    Change = change,
                    DateTime = DateTime.UtcNow,
                    Id = session.SessionId,
                    Memo = session.WalletTransaction.Memo,
                    Payment = session.WalletTransaction.Payment,
                    Reward = reward,
                    RecipientAddress = session.WalletTransaction.RecipientAddress,
                    SenderAddress = session.WalletTransaction.SenderAddress,
                    Spending = tx.Commitment,
                    Spent = change == 0,
                    Delay = session.WalletTransaction.Delay
                };
                SessionAddOrUpdate(session);
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
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private List<Balance> AddBalances(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var balances = new List<Balance>();
            try
            {
                var session = Session(sessionId);
                var (_, scan) = Unlock(session.SessionId);
                var walletTransactions = session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToList();
                if (walletTransactions?.Any() != true)
                {
                    return Enumerable.Empty<Balance>().ToList();
                }

                balances.AddRange(from balanceSheet in walletTransactions
                                  from output in balanceSheet.Transaction.Vout
                                  let keyImage = GetKeyImage(sessionId, output)
                                  where keyImage != null
                                  let spent = WalletTransactionSpent(sessionId, keyImage)
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
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<WalletTransaction> CreateTransaction(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            while (_safeguardDownloadingFlagProvider.IsDownloading)
            {
                Thread.Sleep(100);
            }

            var session = Session(sessionId).EnforceDbExists();
            session.LastError = null;

            SyncWallet(session.SessionId).GetAwaiter().GetResult();

            var calculated = CalculateChange(session.SessionId);
            if (!calculated.Success)
            {
                SetLastError(session, calculated);
                return TaskResult<WalletTransaction>.CreateFailure(calculated.Exception);
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

            m = M(sessionId, blinds, sk, nRows, nCols, index, m, pcmIn, pkIn);
            
            var payment = session.WalletTransaction.Payment;
            var change = session.WalletTransaction.Change;
            
            blinds[1] = pedersen.BlindSwitch(payment, secp256K1.CreatePrivateKey());
            blinds[2] = pedersen.BlindSwitch(change, secp256K1.CreatePrivateKey());
            
            pcmOut[0] = pedersen.Commit(payment, blinds[1]);
            pcmOut[1] = pedersen.Commit(change, blinds[2]);

            var commitSumBalance = pedersen.CommitSum(new List<byte[]> { pcmOut[0], pcmOut[1] },
                new List<byte[]>());
            if (!pedersen.VerifyCommitSum(new List<byte[]> { commitSumBalance },
                new List<byte[]> { pcmOut[0], pcmOut[1] }))
            {
                return TaskResult<WalletTransaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Verify commit sum failed."
                }));
            }

            var bulletChange = BulletProof(change, blinds[2], pcmOut[1]);
            if (!bulletChange.Success)
            {
                return TaskResult<WalletTransaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = bulletChange.Exception.Message
                }));
            }

            var success = mlsag.Prepare(m, blindSum, pcmOut.Length, pcmOut.Length, nCols, nRows, pcmIn, pcmOut,
                blinds);
            if (!success)
            {
                return TaskResult<WalletTransaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Prepare failed."
                }));
            }

            sk[nRows - 1] = blindSum;

            success = mlsag.Generate(ki, pc, ss, randSeed, preimage, nCols, nRows, index, sk, m);
            if (!success)
            {
                return TaskResult<WalletTransaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Generate failed."
                }));
            }

            success = mlsag.Verify(preimage, nCols, nRows, m, ki, pc, ss);
            if (!success)
            {
                return TaskResult<WalletTransaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Verify failed."
                }));
            }

            var offsets = Offsets(pcmIn, nCols);

            var generateTransaction = GenerateTransaction(session.SessionId, m, nCols, pcmOut, blinds, preimage, pc, ki, ss,
                bulletChange.Result.proof, offsets);
            if (!generateTransaction.Success)
            {
                return TaskResult<WalletTransaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = $"Unable to make the transaction. Inner error message {generateTransaction.NonSuccessMessage.message}"
                }));
            }

            var saved = Save(session.SessionId, session.WalletTransaction);
            if (!saved.Success)
                return TaskResult<WalletTransaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Unable to save the transaction."
                }));

            return TaskResult<WalletTransaction>.CreateSuccess(session.WalletTransaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
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
        private TaskResult<bool> GenerateTransaction(Guid sessionId, byte[] m, int nCols, Span<byte[]> pcmOut,
            Span<byte[]> blinds, byte[] preimage, byte[] pc, byte[] ki, byte[] ss, byte[] bp, byte[] offsets)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            try
            {
                var session = Session(sessionId);

                var (outPkPayment, stealthPayment) = StealthPayment(session.WalletTransaction.RecipientAddress);
                var (outPkChange, stealthChange) = StealthPayment(session.WalletTransaction.SenderAddress);

                var changeLockTime = new LockTime(Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow.AddMinutes(10)));

                var tx = new Transaction
                {
                    Bp = new[] {new Bp {Proof = bp}},
                    Mix = nCols,
                    Rct = new[] {new RCT {I = preimage, M = m, P = pc, S = ss}},
                    Ver = 0x2,
                    Vin = new[] {new Vin {Key = new KeyOffsetImage {KImage = ki, KOffsets = offsets}}},
                    Vout = new[]
                    {
                        new Vout
                        {
                            A = session.SessionType == SessionType.Coinstake ? session.WalletTransaction.Payment : 0,
                            C = pcmOut[0],
                            E = stealthPayment.Metadata.EphemKey.ToBytes(),
                            N = ScanPublicKey(session.WalletTransaction.RecipientAddress).Encrypt(
                                Transaction.Message(session.WalletTransaction.Payment, 0, blinds[1],
                                    session.WalletTransaction.Memo)),
                            P = outPkPayment.ToBytes(),
                            T = session.SessionType == SessionType.Coin ? CoinType.Payment : CoinType.Coinstake
                        },
                        new Vout
                        {
                            A = 0,
                            C = pcmOut[1],
                            E = stealthChange.Metadata.EphemKey.ToBytes(),
                            L = changeLockTime.Value,
                            N = ScanPublicKey(session.WalletTransaction.SenderAddress).Encrypt(
                                Transaction.Message(session.WalletTransaction.Change, session.WalletTransaction.Payment,
                                    blinds[2], session.WalletTransaction.Memo)),
                            P = outPkChange.ToBytes(),
                            S = new Script(Op.GetPushOp(changeLockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY)
                                .ToString(),
                            T = CoinType.Change
                        }
                    },
                    Id = session.SessionId
                };
                if (session.SessionType == SessionType.Coinstake)
                {
                    using var secp256K1 = new Secp256k1();
                    using var pedersen = new Pedersen();

                    var (outPkReward, stealthReward) = StealthPayment(session.WalletTransaction.SenderAddress);
                    var rewardLockTime = new LockTime(Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow.AddHours(21)));
                    var blind = pedersen.BlindSwitch(session.WalletTransaction.Reward, secp256K1.CreatePrivateKey());
                    var commit = pedersen.Commit(session.WalletTransaction.Reward, blind);
                    var vOutput = tx.Vout.ToList();
                    vOutput.Insert(0,
                        new Vout
                        {
                            A = session.WalletTransaction.Reward,
                            C = commit,
                            E = stealthReward.Metadata.EphemKey.ToBytes(),
                            L = rewardLockTime.Value,
                            N = ScanPublicKey(session.WalletTransaction.SenderAddress).Encrypt(Transaction.Message(
                                session.WalletTransaction.Reward, 0, blind, session.WalletTransaction.Memo)),
                            P = outPkReward.ToBytes(),
                            S = new Script(Op.GetPushOp(rewardLockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY)
                                .ToString(),
                            T = CoinType.Coinbase
                        });

                    tx.Vout = vOutput.ToArray();
                }

                var generateTransactionTime = GenerateTransactionTime(tx, session.SessionId);
                if (!generateTransactionTime.Success)
                {
                    throw new Exception("Unable to generate the transaction time");
                }

                generateTransactionTime.Result.TxnId = tx.ToHash();
                session.WalletTransaction.Transaction = generateTransactionTime.Result;

                SessionAddOrUpdate(session);
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
        /// <param name="transaction"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private TaskResult<Transaction> GenerateTransactionTime(Transaction transaction, Guid sessionId)
        {
            try
            {
                var session = Session(sessionId);
                var txMessage = transaction.ToHash().ByteToHex();
                var x = System.Numerics.BigInteger.Parse(txMessage,
                    System.Globalization.NumberStyles.AllowHexSpecifier);
                if (x.Sign <= 0)
                {
                    x = -x;
                }

                var timer = new Stopwatch();
                var t = (int)(session.WalletTransaction.Delay * 2.7 * 1000);
                timer.Start();
                var nonce = Cryptography.Sloth.Eval(t, x);
                timer.Stop();
                var y = System.Numerics.BigInteger.Parse(nonce);
                var success = Cryptography.Sloth.Verify(t, x, y);
                if (!success)
                {
                    {
                        return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                        {
                            success = false,
                            message = "Unable to verify the verified delayed function"
                        }));
                    }
                }

                if (timer.Elapsed.Seconds < 5)
                {
                    session.WalletTransaction.Delay++;
                    GenerateTransactionTime(transaction, session.SessionId);
                }

                var lockTime = Util.GetAdjustedTimeAsUnixTimestamp() & ~timer.Elapsed.Seconds;
                transaction.Vtime = new Vtime
                {
                    I = t,
                    M = txMessage.HexToByte(),
                    N = nonce.ToBytes(),
                    W = timer.Elapsed.Ticks,
                    L = lockTime,
                    S = new Script(Op.GetPushOp(lockTime), OpcodeType.OP_CHECKLOCKTIMEVERIFY).ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<Transaction>.CreateSuccess(transaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="blinds"></param>
        /// <param name="sk"></param>
        /// <param name="nRows"></param>
        /// <param name="nCols"></param>s
        /// <param name="index"></param>
        /// <param name="m"></param>
        /// <param name="pcmIn"></param>
        /// <param name="pkIn"></param>
        /// <returns></returns>
        private unsafe byte[] M(Guid sessionId, Span<byte[]> blinds, Span<byte[]> sk, int nRows, int nCols, int index,
            byte[] m, Span<byte[]> pcmIn, Span<byte[]> pkIn)
        {
            using var pedersen = new Pedersen();
        
            var session = Session(sessionId);
            var transactions = SafeguardService.GetTransactions().ToArray();
            transactions.Shuffle();
            
            for (var k = 0; k < nRows - 1; ++k)
                for (var i = 0; i < nCols; ++i)
                {
                    if (i == index)
                    {
                        var (spend, scan) = Unlock(session.SessionId);
                        var message = Transaction.Message(session.WalletTransaction.Spending, scan);
                        var oneTimeSpendKey = spend.Uncover(scan, new PubKey(session.WalletTransaction.Spending.E));
        
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

                    transactions[i].Vout.Shuffle();
                    
                    pcmIn[i + k * nCols] = transactions[i].Vout[0].C;
                    pkIn[i + k * nCols] = transactions[i].Vout[0].P;
        
                    fixed (byte* mm = m, pk = pkIn[i + k * nCols])
                    {
                        Libsecp256k1Zkp.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
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
        /// <param name="path"></param>
        /// <returns></returns>
        private static KeyPath IncrementKeyPath(string path)
        {
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty().NotWhiteSpace();

            var keyPath = new KeyPath(path);
            return keyPath.Increment();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <param name="passphrase"></param>
        /// <param name="concatenateMnemonic"></param>
        /// <param name="hdRoot"></param>
        private static void CreateHdRootKey(SecureString mnemonic, SecureString passphrase,
            out string concatenateMnemonic, out ExtKey hdRoot)
        {
            Guard.Argument(mnemonic, nameof(mnemonic)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            concatenateMnemonic = string.Join(" ", mnemonic.ToUnSecureString());
            hdRoot = new Mnemonic(concatenateMnemonic).DeriveExtKey(passphrase.ToUnSecureString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<BalanceSheet[]> History(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId).EnforceDbExists();
            
            SyncWallet(session.SessionId).GetAwaiter().GetResult();

            var balanceSheets = new List<BalanceSheet>();
            var walletTransactions = session.Database.Query<WalletTransaction>().OrderBy(x => x.DateTime).ToList();
            if (walletTransactions?.Any() != true)
            {
                return TaskResult<BalanceSheet[]>.CreateFailure("Unable to find any wallet transactions");
            }
            
            try
            {
                var (_, scan) = Unlock(session.SessionId);
                ulong received = 0;

                foreach (var transaction in walletTransactions.Select(x => x.Transaction))
                {
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
                                        messagePayment.Amount, 0, received, payment, transaction.TxnId.ByteToHex())); 
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
                                transaction.TxnId.ByteToHex()));
                                
                            continue;
                        }
                            
                        var messageChange = Transaction.Message(change.ElementAt(0), scan);
                        received -= messageChange.Paid;
                        balanceSheets.Add(MoneyBalanceSheet(messageChange.Date, messageChange.Memo,
                            messageChange.Paid, 0, 0, received, change, transaction.TxnId.ByteToHex()));
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
                return TaskResult<BalanceSheet[]>.CreateFailure(ex.Message);
            }

            return TaskResult<BalanceSheet[]>.CreateSuccess(balanceSheets.OrderBy(x => x.Date).ToArray());
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
        /// <returns></returns>
        private static BalanceSheet MoneyBalanceSheet(DateTime dateTime, string memo, ulong sent, ulong received,
            ulong reward, ulong balance, Vout[] outputs, string txId)
        {
            var balanceSheet = new BalanceSheet
            {
                Date = dateTime.ToShortDateString(),
                Memo = memo,
                Balance = balance.DivWithNaT().ToString("F9"),
                Outputs = outputs,
                TxId = txId
            };
            if (sent != 0)
            {
                balanceSheet.MoneyOut = $"-{sent.DivWithNaT():F9}";
            }
            if (received != 0)
            {
                balanceSheet.MoneyIn = $"{received.DivWithNaT():F9}";
            }
            if (reward != 0)
            {
                balanceSheet.Reward = $"{reward.DivWithNaT():F9}";
            }
            return balanceSheet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private Vout GetLasTransactionSpent(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            var walletTransactionMessages = new List<WalletTransactionMessage>();
            var (_, scan) = Unlock(session.SessionId);
            var walletTransactions = session.Database.Query<WalletTransaction>().ToArray();
            foreach (var output in walletTransactions.SelectMany(x =>
                x.Transaction.Vout.Where(z => z.T is CoinType.Change)))
            {
                try
                {
                    var keyImage = GetKeyImage(session.SessionId, output);
                    if (keyImage == null) continue;
                    var spent = WalletTransactionSpent(session.SessionId, keyImage);
                    if (spent) continue;

                    walletTransactionMessages.Add(Transaction.Message(output, scan));
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            var transactionMessages = walletTransactionMessages.OrderByDescending(x => x.Amount);
            return transactionMessages.Last().Output;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public int Count(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId);
            return session.Database.Query<WalletTransaction>().ToList().Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public KeySet LastKeySet(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId);
            var keySet = session.Database.Query<KeySet>().ToList().LastOrDefault();

            return keySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public KeySet KeySet(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId);
            var keySet = session.Database.Query<KeySet>().FirstOrDefault();
            session.WalletTransaction ??= new WalletTransaction();
            session.WalletTransaction.SenderAddress = keySet.StealthAddress;
            SessionAddOrUpdate(session);

            return keySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public KeySet NextKeySet(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            KeySet keySet = default;
            try
            {
                var session = Session(sessionId);
                keySet = KeySet(session.SessionId);
                var txCount = Count(session.SessionId);
                if (txCount > 0)
                {
                    keySet.KeyPath = IncrementKeyPath(keySet.KeyPath).ToString();
                    session.Database.Update(keySet);
                }
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error getting next key set");
            }

            return keySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public (PubKey, StealthPayment) StealthPayment(string address)
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
        public PubKey ScanPublicKey(string address)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty().NotWhiteSpace();

            var stealth = new BitcoinStealthAddress(address, _network);
            return stealth.ScanPubKey;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public async Task<TaskResult<WalletTransaction>> ReceivePayment(Guid sessionId, string paymentId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(paymentId, nameof(paymentId)).NotNull().NotEmpty().NotWhiteSpace();

            var session = Session(sessionId).EnforceDbExists();

            try
            {
                await SyncWallet(session.SessionId);

                if (AlreadyReceivedPayment(paymentId, session, out var taskResult)) return taskResult;
                
                var baseAddress = _client.GetBaseAddress();
                if (baseAddress == null)
                {
                    throw new Exception("Cannot get base address");
                }
                
                var path = string.Format(_networkSettings.Routing.TransactionId, paymentId);
                var genericResponse = await _client.GetAsync<Transaction>(baseAddress, path,
                    new CancellationToken());
                if (genericResponse == null)
                {
                    var output = TaskResult<WalletTransaction>.CreateFailure(
                        new Exception($"Failed to find transaction with paymentId: {paymentId}"));
                    SetLastError(session, output);
                    return output;
                }
                if (genericResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    var output = TaskResult<WalletTransaction>.CreateFailure(
                        new Exception($"Failed to find transaction with paymentId: {paymentId}"));
                    SetLastError(session, output);
                    return output;
                }
                
                var (spend, scan) = Unlock(session.SessionId);
                var outputs = (from v in genericResponse.Data.Vout
                               let uncover = spend.Uncover(scan, new PubKey(v.E))
                               where uncover.PubKey.ToBytes().SequenceEqual(v.P)
                               select v.Cast<Vout>()).ToList();
                if (outputs.Any() != true)
                {
                    var emptyPayment =
                        TaskResult<WalletTransaction>.CreateFailure(
                            new Exception("Your stealth address does not control this payment"));
                    SetLastError(session, emptyPayment);
                    return emptyPayment;
                }

                session.WalletTransaction = new WalletTransaction
                {
                    SenderAddress = session.WalletTransaction.SenderAddress,
                    DateTime = DateTime.UtcNow,
                    Transaction = new Transaction
                    {
                        Bp = genericResponse.Data.Bp,
                        Mix = genericResponse.Data.Mix,
                        Rct = genericResponse.Data.Rct,
                        TxnId = genericResponse.Data.TxnId,
                        Vtime =  genericResponse.Data.Vtime,
                        Vout = outputs.ToArray(),
                        Vin = genericResponse.Data.Vin,
                        Ver = genericResponse.Data.Ver
                    },
                    WalletType = WalletType.Receive
                };

                SessionAddOrUpdate(session);

                var saved = Save(session.SessionId, session.WalletTransaction);
                if (saved.Success) return TaskResult<WalletTransaction>.CreateSuccess(session.WalletTransaction);

                SetLastError(session, saved);
                return TaskResult<WalletTransaction>.CreateFailure(saved);
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error receiving payment");
                var message = ex.Message;
                if (ex is UriFormatException)
                {
                    message = "appsettings.json api_gateway:{endpoint} -> " + message;
                }

                var output = TaskResult<WalletTransaction>.CreateFailure(new Exception($"{message}"));
                SetLastError(session, output);
                return output;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public byte[] GetKeyImage(Guid sessionId, Vout output)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(output, nameof(output)).NotNull();

            var (spend, scan) = Unlock(sessionId);
            var oneTimeSpendKey = spend.Uncover(scan, new PubKey(output.E));
            var mlsag = new MLSAG();

            return mlsag.ToKeyImage(oneTimeSpendKey.ToHex().HexToByte(), oneTimeSpendKey.PubKey.ToBytes());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task<TaskResult<bool>> Send(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId).EnforceDbExists();
            session.LastError = null;

            Transaction transaction = null;
            try
            {
                transaction = GetTransaction(session.SessionId);

                var baseAddress = _client.GetBaseAddress();
                if (baseAddress == null)
                {
                    throw new Exception("Cannot get base address");
                }

                var postedStatusCode =
                    await _client.PostAsync(transaction, baseAddress, _networkSettings.Routing.Transaction, new CancellationToken());
                if (postedStatusCode == HttpStatusCode.OK) return TaskResult<bool>.CreateSuccess(true);

                var fail = TaskResult<bool>.CreateFailure(
                    new Exception($"Unable to send transaction with paymentId: {transaction.TxnId.ByteToHex()}"));
                SetLastError(session, fail);
                RollBackTransaction(session, transaction.Id);

                return fail;
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error sending transaction");
                var message = ex.Message;
                if (ex is UriFormatException)
                {
                    message = "appsettings.json api_gateway:{endpoint} -> " + message;
                }

                var output = TaskResult<bool>.CreateFailure(new Exception($"{message}"));
                SetLastError(session, output);
                if (transaction != null) RollBackTransaction(session, transaction.Id);
                return output;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task SyncWallet(Guid sessionId, int n = 3)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            var session = Session(sessionId).EnforceDbExists();
            var walletTransactions = session.Database.Query<WalletTransaction>().ToList().OrderBy(d => d.DateTime)
                .TakeLast(n).ToArray();
            if (walletTransactions.Any())
            {
                await Task.Factory.StartNew(async () =>
                {
                    foreach (var (walletTransaction, index) in walletTransactions.Select((value, i) => (value, i)))
                    {
                        var count = walletTransactions.Length == 0 ? 0 : walletTransactions.Length - 1;
                        var currentRetry = 0;
                        var jitter = new Random();
                        for (;;)
                        {
                            _logger.Here().Debug("Syncing wallet");
                            try
                            {
                                var baseAddress = _client.GetBaseAddress();
                                if (baseAddress == null)
                                {
                                    throw new Exception("Cannot get base address");
                                }

                                var path = string.Format(_networkSettings.Routing.TransactionId,
                                    walletTransaction.Transaction.TxnId.ByteToHex());
                                var genericResponse =
                                    await _client.GetAsync<Transaction>(baseAddress, path, new CancellationToken());
                                if (genericResponse.Data != null && genericResponse.HttpStatusCode == HttpStatusCode.OK)
                                {
                                    if (index == count)
                                    {
                                        _resetEvent.Set();
                                    }

                                    break;
                                }

                                if (genericResponse.HttpStatusCode is HttpStatusCode.NotFound)
                                {
                                    goto NotFound;
                                }

                                if (genericResponse.HttpStatusCode is HttpStatusCode.ServiceUnavailable)
                                {
                                    if (index == count)
                                    {
                                        _resetEvent.Set();
                                    }

                                    break;
                                }

                                NotFound:
                                currentRetry++;
                                var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, currentRetry)) +
                                                 TimeSpan.FromMilliseconds(jitter.Next(0, 1000));

                                _logger.Here().Debug("Retrying {@CurrentRetry} {@RetryDelay}", currentRetry,
                                    retryDelay);

                                if (retryDelay.Minutes * 60 + retryDelay.Seconds < 60)
                                {
                                    await Task.Delay(retryDelay);
                                    continue;
                                }
                                
                                var rolledBack = RollBackTransaction(session, walletTransaction.Transaction.Id);
                                if (!rolledBack.Success)
                                {
                                    _logger.Here().Error(rolledBack.Exception.Message);
                                }

                                if (index == count)
                                {
                                    _resetEvent.Set();
                                }

                                break;
                            }
                            catch (Exception ex)
                            {
                                _logger.Here().Error(ex, "Error syncing wallet");
                                _resetEvent.Set();
                                break;
                            }
                        }
                    }
                });
                _resetEvent.WaitOne();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public async Task<TaskResult<bool>> RecoverTransactions(Guid sessionId, int start)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(start, nameof(start)).NotNegative();
            var session = Session(sessionId).EnforceDbExists();
            try
            {
                using (var db = Util.LiteRepositoryFactory(session.Identifier, session.Passphrase))
                {
                    var wExists = db.Query<WalletTransaction>().Exists();
                    if (wExists)
                    {
                        var dropped = db.Database.DropCollection($"{nameof(WalletTransaction)}");
                        if (!dropped)
                        {
                            var message = $"Unable to drop collection for {nameof(WalletTransaction)}";
                            _logger.Here().Error(message);
                            return TaskResult<bool>.CreateFailure(new Exception(message));
                        }
                    }
                }

                var baseAddress = _client.GetBaseAddress();
                if (baseAddress == null)
                {
                    throw new Exception("Cannot get base address");
                }

                var blockHeight = await _client.GetBlockHeightAsync(baseAddress, _networkSettings.Routing.BlockHeight, new CancellationToken());
                if (blockHeight == null)
                {
                    var output = TaskResult<bool>.CreateFailure(new Exception("Failed to find any blocks"));
                    SetLastError(session, output);
                    return output;
                }

                var height = (int)blockHeight.Height;
                const int maxBlocks = 10;
                var chunks = Enumerable.Repeat(maxBlocks, (height / maxBlocks)).ToList();
                if (height % maxBlocks != 0) chunks.Add(height % maxBlocks);
                foreach (var chunk in chunks)
                {
                    var path = string.Format(_networkSettings.Routing.Blocks, start, chunk);
                    var blocks = await _client.GetRangeAsync<Block>(baseAddress, path, new CancellationToken());
                    if (blocks != null)
                    {
                        foreach (var transaction in blocks.Data.SelectMany(x => x.Txs))
                        {
                            var (spend, scan) = Unlock(session.SessionId);
                            var outputs = (from v in transaction.Vout
                                           let uncover = spend.Uncover(scan, new PubKey(v.E))
                                           where uncover.PubKey.ToBytes().Xor(v.P)
                                           select v.Cast<Vout>()).ToList();
                            if (outputs.Any() != true)
                            {
                                continue;
                            }

                            session.WalletTransaction = new WalletTransaction
                            {
                                Id = sessionId,
                                SenderAddress = session.WalletTransaction.SenderAddress,
                                DateTime = DateTime.UtcNow,
                                Transaction = new Transaction
                                {
                                    Id = sessionId,
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
                                Delay = 5
                            };
                            SessionAddOrUpdate(session);
                            var saved = Save(session.SessionId, session.WalletTransaction);
                            if (!saved.Success)
                            {
                                _logger.Here().Error("Unable to save transaction: {@Transaction}", transaction.TxnId.ByteToHex());
                            }

                            session = SessionAddOrUpdate(new Session(session.Identifier, session.Passphrase));
                        }
                    }

                    start += chunk;
                }

                return TaskResult<bool>.CreateSuccess(true);
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "Error recovering transactions");
                return TaskResult<bool>.CreateSuccess(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public (Key, Key) Unlock(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            Key spend = null;
            Key scan = null;

            try
            {
                var keySet = KeySet(sessionId);
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
        /// <param name="sessionId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public TaskResult<bool> Save<T>(Guid sessionId, T data)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(data, nameof(data)).NotEqual(default);

            try
            {
                var session = Session(sessionId);
                session.Database.Insert(data);
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
        /// <param name="id"></param>
        /// <returns></returns>
        public TaskResult<bool> RollBackTransaction(Session session, Guid id)
        {
            Guard.Argument(session, nameof(session)).NotNull();
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
        /// <param name="taskResult"></param>
        /// <returns></returns>
        private bool AlreadyReceivedPayment(string paymentId, Session session,
            out TaskResult<WalletTransaction> taskResult)
        {
            taskResult = null;
            var walletTransactions = session.Database.Query<WalletTransaction>().ToList();
            if (!walletTransactions.Any()) return false;
            var walletTransaction = walletTransactions.FirstOrDefault(x => x.Transaction.TxnId.Xor(paymentId.HexToByte()));
            if (walletTransaction == null) return false;
            var output = TaskResult<WalletTransaction>.CreateFailure(
                new Exception($"Transaction with paymentId: {paymentId} already exists"));
            SetLastError(session, output);
            {
                taskResult = output;
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        private bool WalletTransactionSpent(Guid sessionId, byte[] image)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(image, nameof(image)).NotNull().MaxCount(33);

            var spent = false;
            var session = Session(sessionId);
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="obj"></param>
        private void SetLastError<T>(Session session, TaskResult<T> obj)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(obj, nameof(obj)).NotNull();

            if (obj.Exception == null)
            {
                session.LastError = obj.NonSuccessMessage;
                _logger.Here().Error("Last session error: {@Error}", obj.NonSuccessMessage.message);
            }
            else
            {
                session.LastError = JObject.FromObject(new
                {
                    success = false,
                    message = obj.Exception.Message
                });

                _logger.Here().Error("Last session error: {@Error}", obj.Exception.Message);
            }

            SessionAddOrUpdate(session);
        }
    }
}