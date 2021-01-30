// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json.Linq;

using Dawn;

using NBitcoin;
using NBitcoin.Stealth;

using Transaction = BAMWallet.Model.Transaction;
using Util = BAMWallet.Helper.Util;
using BAMWallet.Helper;
using BAMWallet.Model;
using BAMWallet.Rpc;
using BAMWallet.Extentions;
using BAMWallet.Services;

using Libsecp256k1Zkp.Net;

namespace BAMWallet.HD
{
    public class WalletService : IWalletService
    {
        private const string HDPath = "m/44'/847177'/0'/0/";
        private const int FeeNByte = 2564;

        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagProvider;
        private readonly ILogger _logger;
        private readonly NBitcoin.Network _network;
        private readonly Client _client;
        private readonly IConfigurationSection _apiGatewaySection;

        private ConcurrentDictionary<Guid, Session> Sessions { get; }

        public WalletService(ISafeguardDownloadingFlagProvider safeguardDownloadingFlagProvider, IConfiguration configuration, ILogger<WalletService> logger)
        {
            _safeguardDownloadingFlagProvider = safeguardDownloadingFlagProvider;

            var apiNetworkSection = configuration.GetSection(Constant.Network);
            var environment = apiNetworkSection.GetValue<string>(Constant.Environment);

            _network = environment == Constant.Mainnet ? NBitcoin.Network.Main : NBitcoin.Network.TestNet;
            _logger = logger;
            _apiGatewaySection = configuration.GetSection(RestCall.Gateway);
            _client = new Client(configuration, _logger);

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

            var mSession = Sessions.AddOrUpdate(session.SessionId, session, (Key, existingVal) =>
            {
                if (session != existingVal)
                    throw new ArgumentException("Duplicate sessions are not allowed: {0}.", session.SessionId.ToString());

                try
                {
                    existingVal.WalletTransaction.Balance = session.WalletTransaction.Balance;
                    existingVal.WalletTransaction.Change = session.WalletTransaction.Change;
                    existingVal.WalletTransaction.DateTime = session.WalletTransaction.DateTime;
                    existingVal.WalletTransaction.Fee = session.WalletTransaction.Fee;
                    existingVal.WalletTransaction.Id = session.SessionId;
                    existingVal.WalletTransaction.Memo = session.WalletTransaction.Memo;
                    existingVal.WalletTransaction.Payment = session.WalletTransaction.Payment;
                    existingVal.WalletTransaction.RecipientAddress = session.WalletTransaction.RecipientAddress;
                    existingVal.WalletTransaction.SenderAddress = session.WalletTransaction.SenderAddress;
                    existingVal.WalletTransaction.Spent = session.WalletTransaction.Spent;
                    existingVal.WalletTransaction.TxId = session.WalletTransaction.TxId ?? Array.Empty<byte>();
                    existingVal.WalletTransaction.Vout = session.WalletTransaction.Vout ?? null;
                    existingVal.WalletTransaction.WalletType = session.WalletTransaction.WalletType;
                }
                catch (Exception)
                { }

                return existingVal;
            });

            return mSession;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<ulong> AvailableBalance(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            ulong balance;

            try
            {
                var session = Session(sessionId);

                var walletTxns = session.Database.Query<WalletTransaction>().OrderBy(d => d.DateTime).ToList();
                if (walletTxns?.Any() != true)
                {
                    return TaskResult<ulong>.CreateSuccess(0);
                }

                balance = Balance(walletTxns, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return TaskResult<ulong>.CreateFailure(ex);
            }

            return TaskResult<ulong>.CreateSuccess(balance);
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
                var keySet = CreateKeySet(new KeyPath($"m/44'/847177'/{index}'/0/0"), next.RootKey.HexToByte(), next.ChainCode.HexToByte());

                session.Database.Insert(keySet);

                next.ChainCode.ZeroString();
                next.RootKey.ZeroString();

                keySet.RootKey.ZeroString();
                keySet.ChainCode.ZeroString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
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

            var walletId = NewID(16);

            walletId.MakeReadOnly();
            mnemonic.MakeReadOnly();
            passphrase.MakeReadOnly();

            CreateHDRootKey(mnemonic, passphrase, out string concatenateMnemonic, out ExtKey hdRoot);

            concatenateMnemonic.ZeroString();

            var keySet = CreateKeySet(new KeyPath($"{HDPath}0"), hdRoot.PrivateKey.ToHex().HexToByte(), hdRoot.ChainCode);

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
                _logger.LogError(ex.Message);
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
        public SecureString NewID(int bytes = 32)
        {
            using var secp256k1 = new Secp256k1();

            var secureString = new SecureString();
            foreach (var c in $"id_{secp256k1.RandomSeed(bytes).ByteToHex()}") secureString.AppendChar(c);
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
        /// <param name="address"></param>
        /// <returns></returns>
        public ulong TotalAmount(Guid sessionId, string address)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty().NotWhiteSpace();

            ulong total;

            var session = Session(sessionId);
            var txns = session.Database.Query<WalletTransaction>().Where(x => x.SenderAddress == address).ToEnumerable();
            if (txns?.Any() != true)
            {
                return 0;
            }

            var outputs = txns.Select(a => a.Change);
            total = Util.Sum(outputs);

            return total;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        public WalletTransaction LastWalletTransaction(Guid sessionId, WalletType transactionType)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId);

            WalletTransaction walletTx;

            var transactions = session.Database.Query<WalletTransaction>().Where(x => x.Id == session.SessionId && x.WalletType == transactionType).ToList();
            if (transactions?.Any() != true)
            {
                return null;
            }

            walletTx = transactions.Last();

            return walletTx;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public Transaction Transaction(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId);

            Transaction transaction = null;

            transaction = session.Database.Query<Transaction>().Where(x => x.Id == session.SessionId).FirstOrDefault();

            return transaction;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nByte"></param>
        /// <returns></returns>
        public ulong Fee(int nByte)
        {
            return ((double)0.000012 * nByte).ConvertToUInt64();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TaskResult<IEnumerable<string>> WalletList()
        {
            var wallets = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "wallets");
            string[] files;

            try
            {
                files = Directory.GetFiles(wallets, "*.db");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
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
            if (keys == null)
                return Enumerable.Empty<KeySet>();

            return keys;
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
                var session = Session(sessionId);

                var keys = KeySets(session.SessionId);
                if (keys != null)
                    addresses = keys.Select(k => k.StealthAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
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

                List<WalletTransaction> walletTransactions;
                walletTransactions = session.Database.Query<WalletTransaction>().ToList();
                if (walletTransactions?.Any() != true)
                {
                    return TaskResult<bool>.CreateFailure(new Exception("There are no wallet transactions."));
                }

                var changeList = new Dictionary<ulong, WalletTransaction>();

                var (spend, scan) = Unlock(session.SessionId);

                foreach (var transaction in walletTransactions)
                {
                    if (transaction.Change == 0)
                    {
                        var change = Util.MessageAmount(transaction.Vout[0], scan);
                        changeList.Add(change, transaction);

                        transaction.Change = change;

                        continue;
                    }

                    changeList.Add(Util.MessageAmount(transaction.Vout[2], scan), transaction);
                }

                var fee = session.SessionType == SessionType.Coin ? Fee(FeeNByte) : 0;
                var payment = session.WalletTransaction.Payment;
                var reward = session.SessionType == SessionType.Coinstake ? session.WalletTransaction.Reward : 0;
                var balance = AvailableBalance(session.SessionId).Result;
                var closest = changeList.OrderByDescending(x => x.Key).Last().Key;
                var vOutChange = changeList.FirstOrDefault(x => x.Key == closest);
                var spending = vOutChange.Value.Vout.FirstOrDefault(x => Util.MessageAmount(x, scan) == closest);

                session.WalletTransaction = new WalletTransaction
                {
                    Balance = balance,
                    Change = balance - payment - fee,
                    DateTime = DateTime.UtcNow,
                    Fee = fee,
                    Id = session.SessionId,
                    Memo = session.WalletTransaction.Memo,
                    Payment = session.WalletTransaction.Payment,
                    Reward = reward,
                    RecipientAddress = session.WalletTransaction.RecipientAddress,
                    SenderAddress = session.WalletTransaction.SenderAddress,
                    Spending = spending,
                    Spent = balance - payment == 0,
                    Vout = vOutChange.Value.Vout
                };

                SessionAddOrUpdate(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<Transaction> CreateTransaction(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            using var secp256k1 = new Secp256k1();
            using var pedersen = new Pedersen();
            using var mlsag = new MLSAG();

            var blinds = new Span<byte[]>(new byte[4][]);
            var sk = new Span<byte[]>(new byte[2][]);
            int nRows = 2; // last row sums commitments
            int nCols = 2; // ring size
            int index = Libsecp256k1Zkp.Net.Util.Rand(0, nCols) % nCols;
            var m = new byte[nRows * nCols * 33];
            var pcm_in = new Span<byte[]>(new byte[nCols * 1][]);
            var pcm_out = new Span<byte[]>(new byte[3][]);
            var randSeed = secp256k1.Randomize32();
            var preimage = secp256k1.Randomize32();
            var pc = new byte[32];
            var ki = new byte[33 * 1];
            var ss = new byte[nCols * nRows * 32];
            var blindSum = new byte[32];
            var blindSumChange = new byte[32];
            var pk_in = new Span<byte[]>(new byte[nCols * 1][]);

            while (_safeguardDownloadingFlagProvider.IsDownloading)
            {
                System.Threading.Thread.Sleep(100);
            }

            m = M(sessionId, blinds, sk, nRows, nCols, index, m, pcm_in, pk_in);

            var session = Session(sessionId);

            var fee = session.WalletTransaction.Fee;
            var payment = session.WalletTransaction.Payment;
            var change = session.WalletTransaction.Change;

            blinds[1] = pedersen.BlindSwitch(fee, secp256k1.CreatePrivateKey());
            blinds[2] = pedersen.BlindSwitch(payment, secp256k1.CreatePrivateKey());
            blinds[3] = pedersen.BlindSwitch(change, secp256k1.CreatePrivateKey());

            pcm_out[0] = pedersen.Commit(fee, blinds[1]);
            pcm_out[1] = pedersen.Commit(payment, blinds[2]);
            pcm_out[2] = pedersen.Commit(change, blinds[3]);

            var commitSumBalance = pedersen.CommitSum(new List<byte[]> { pcm_out[0], pcm_out[1], pcm_out[2] }, new List<byte[]> { });
            if (!pedersen.VerifyCommitSum(new List<byte[]> { commitSumBalance }, new List<byte[]> { pcm_out[0], pcm_out[1], pcm_out[2] }))
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Verify commit sum failed."
                }));
            }

            var bulletChange = BulletProof(change, blinds[3], pcm_out[2]);
            if (!bulletChange.Success)
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = bulletChange.Exception.Message
                }));
            }

            var success = mlsag.Prepare(m, blindSum, pcm_out.Length, pcm_out.Length, nCols, nRows, pcm_in, pcm_out, blinds);
            if (!success)
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Prepare failed."
                }));
            }

            sk[nRows - 1] = blindSum;

            success = mlsag.Generate(ki, pc, ss, randSeed, preimage, nCols, nRows, index, sk, m);
            if (!success)
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Generate failed."
                }));
            }

            success = mlsag.Verify(preimage, nCols, nRows, m, ki, pc, ss);
            if (!success)
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Verify failed."
                }));
            }

            var offsets = Offsets(pcm_in, pk_in, nRows, nCols);
            var transaction = TransactionFactory(session.SessionId, m, nCols, pcm_out, blinds, preimage, pc, ki, ss, bulletChange.Result.proof, offsets);
            var kbOverflow = Util.SerializeProto(transaction).Length > FeeNByte;

            if (!kbOverflow)
            {
                var saved = Save(session.SessionId, transaction);
                if (saved.Success)
                {
                    return TaskResult<Transaction>.CreateSuccess(transaction);
                }
            }

            return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
            {
                success = false,
                message = "Transaction overflow."
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="m"></param>
        /// <param name="nCols"></param>
        /// <param name="pcm_out"></param>
        /// <param name="blinds"></param>
        /// <param name="preimage"></param>
        /// <param name="pc"></param>
        /// <param name="ki"></param>
        /// <param name="ss"></param>
        /// <param name="bp"></param>
        /// <param name="offsets"></param>
        /// <returns></returns>
        private Transaction TransactionFactory(Guid sessionId, byte[] m, int nCols, Span<byte[]> pcm_out, Span<byte[]> blinds, byte[] preimage, byte[] pc, byte[] ki, byte[] ss, byte[] bp, byte[] offsets)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId);

            var (outPkFee, stealthFee) = MakeStealthPayment(session.WalletTransaction.SenderAddress);
            var (outPkPayment, stealthPayment) = MakeStealthPayment(session.WalletTransaction.RecipientAddress);
            var (outPkChange, stealthChange) = MakeStealthPayment(session.WalletTransaction.SenderAddress);

            var feeLockTime = new LockTime(Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow.AddHours(21)));
            var changeLockTime = new LockTime(Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow.AddMinutes(5)));

            var tx = new Transaction
            {
                Bp = new Bp[]
                {
                    new Bp
                    {
                         Proof = bp
                    }
                },
                Mix = nCols,
                Rct = new RCT[]
                {
                    new RCT
                    {
                         I = preimage,
                         M = m,
                         P = pc,
                         S = ss
                    }
                },
                Ver = 0x1,
                Vin = new Vin[]
                {
                    new Vin
                    {
                         Key = new Aux
                         {
                              K_Image = ki,
                              K_Offsets = offsets
                         }
                    }
                },
                Vout = new Vout[]
                {
                   new Vout
                   {
                        A = session.SessionType == SessionType.Coin ? session.WalletTransaction.Fee : session.WalletTransaction.Reward,
                        C = pcm_out[0],
                        E = stealthFee.Metadata.EphemKey.ToBytes(),
                        L = feeLockTime.Value,
                        N = ScanPublicKey(session.WalletTransaction.SenderAddress).Encrypt(Util.Message(session.SessionType == SessionType.Coin ? session.WalletTransaction.Fee : session.WalletTransaction.Reward, blinds[1], string.Empty)),
                        P = outPkFee.ToBytes(),
                        S = new Script(Op.GetPushOp(feeLockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY).ToString(),
                        T = session.SessionType == SessionType.Coin ? CoinType.fee : CoinType.Coinbase
                   },
                   new Vout
                   {
                        A = session.SessionType == SessionType.Coinstake ? session.WalletTransaction.Payment : 0,
                        C = pcm_out[1],
                        E = stealthPayment.Metadata.EphemKey.ToBytes(),
                        N = ScanPublicKey(session.WalletTransaction.RecipientAddress).Encrypt(Util.Message(session.WalletTransaction.Payment, blinds[2], session.WalletTransaction.Memo)),
                        P = outPkPayment.ToBytes(),
                        S = null,
                        T = session.SessionType == SessionType.Coin ? CoinType.Coin : CoinType.Coinstake
                    },
                    new Vout
                    {
                        A = 0,
                        C = pcm_out[2],
                        E = stealthChange.Metadata.EphemKey.ToBytes(),
                        L = changeLockTime.Value,
                        N = ScanPublicKey(session.WalletTransaction.SenderAddress).Encrypt(Util.Message(session.WalletTransaction.Change, blinds[3], string.Empty)),
                        P = outPkChange.ToBytes(),
                        S = new Script(Op.GetPushOp(changeLockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY).ToString(),
                        T = CoinType.Coin
                    }
                }
            };

            tx.Id = session.SessionId;
            tx.TxnId = tx.ToHash();

            session.WalletTransaction.TxId = tx.TxnId;
            session.WalletTransaction.Vout = tx.Vout;

            SessionAddOrUpdate(session);

            return tx;
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
        /// <param name="pcm_in"></param>
        /// <param name="pk_in"></param>
        /// <returns></returns>
        private unsafe byte[] M(Guid sessionId, Span<byte[]> blinds, Span<byte[]> sk, int nRows, int nCols, int index, byte[] m, Span<byte[]> pcm_in, Span<byte[]> pk_in)
        {
            using var pedersen = new Pedersen();

            var session = Session(sessionId);
            var transactions = SafeguardService.GetTransactions();

            for (int k = 0; k < nRows - 1; ++k)
                for (int i = 0; i < nCols; ++i)
                {
                    if (i == index)
                    {
                        var (spend, scan) = Unlock(session.SessionId);
                        var message = Util.Message(session.WalletTransaction.Spending, scan);
                        var oneTimeSpendKey = spend.Uncover(scan, new PubKey(session.WalletTransaction.Spending.E));

                        sk[0] = oneTimeSpendKey.ToHex().HexToByte();
                        blinds[0] = message.Blind;

                        pcm_in[i + k * nCols] = pedersen.Commit(message.Amount, message.Blind);
                        pk_in[i + k * nCols] = oneTimeSpendKey.PubKey.ToBytes();

                        fixed (byte* mm = m, pk = pk_in[i + k * nCols])
                        {
                            Libsecp256k1Zkp.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
                        }

                        continue;
                    }

                    RollRingMember(transactions, pcm_in, pk_in, out Vout vout);

                    pcm_in[i + k * nCols] = vout.C;
                    pk_in[i + k * nCols] = vout.P;

                    fixed (byte* mm = m, pk = pk_in[i + k * nCols])
                    {
                        Libsecp256k1Zkp.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
                    }
                }

            return m;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transactions"></param>
        /// <param name="pcm_in"></param>
        /// <param name="pk_in"></param>
        /// <param name="vout"></param>
        private static void RollRingMember(IEnumerable<Transaction> transactions, Span<byte[]> pcm_in, Span<byte[]> pk_in, out Vout vout)
        {
            var voutIndex = Libsecp256k1Zkp.Net.Util.Rand(0, 2);
            var vouts = transactions.ElementAt(Libsecp256k1Zkp.Net.Util.Rand(0, transactions.Count())).Vout;

            if (pcm_in.IsEmpty != true && pk_in.IsEmpty != true)
            {
                var pcm = pcm_in.GetEnumerator();
                var pk = pk_in.GetEnumerator();

                while (pcm.MoveNext())
                {
                    if (pcm.Current != null)
                    {
                        if (pcm.Current.SequenceEqual(vouts[voutIndex].C))
                        {
                            RollRingMember(transactions, pcm_in, pk_in, out _);
                            break;
                        }
                    }

                    pk.MoveNext();

                    if (pk.Current != null)
                    {
                        if (pk.Current.SequenceEqual(vouts[voutIndex].P))
                        {
                            RollRingMember(transactions, pcm_in, pk_in, out _);
                            break;
                        }
                    }
                }
            }

            vout = vouts[voutIndex];
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
                using var secp256k1 = new Secp256k1();

                proofStruct = bulletProof.GenProof(balance, blindSum, secp256k1.RandomSeed(32), null, null, null);
                var success = bulletProof.Verify(commitSum, proofStruct.proof, null);

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
        /// <param name="pkin"></param>
        /// <param name="nRows"></param>
        /// <param name="nCols"></param>
        /// <returns></returns>
        private unsafe static byte[] Offsets(Span<byte[]> pcin, Span<byte[]> pkin, int nRows, int nCols)
        {
            int i = 0, k = 0;
            byte[] offsets = new byte[nRows * nCols * 33];
            var pcmin = pcin.GetEnumerator();
            var ppkin = pkin.GetEnumerator();

            while (pcmin.MoveNext())
            {
                fixed (byte* pcmm = offsets, pcm = pcmin.Current)
                {
                    Libsecp256k1Zkp.Net.Util.MemCpy(&pcmm[(i + k * nCols) * 33], pcm, 33);
                }

                i++;

                ppkin.MoveNext();

                fixed (byte* pkii = offsets, pkk = ppkin.Current)
                {
                    Libsecp256k1Zkp.Net.Util.MemCpy(&pkii[(i + k * nCols) * 33], pkk, 33);
                }

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

            var keypth = new KeyPath(path);
            return keypth.Increment();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <param name="passphrase"></param>
        /// <param name="concatenateMnemonic"></param>
        /// <param name="hdRoot"></param>
        private static void CreateHDRootKey(SecureString mnemonic, SecureString passphrase, out string concatenateMnemonic, out ExtKey hdRoot)
        {
            Guard.Argument(mnemonic, nameof(mnemonic)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            concatenateMnemonic = string.Join(" ", mnemonic.ToUnSecureString());
            hdRoot = new Mnemonic(concatenateMnemonic).DeriveExtKey(passphrase.ToUnSecureString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private ulong Balance(IEnumerable<WalletTransaction> transactions, Guid sessionId)
        {
            Guard.Argument(transactions, nameof(transactions)).NotNull();
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            ulong total = 0;

            try
            {
                var (spend, scan) = Unlock(sessionId);
                ulong received = 0, fee = 0, payment = 0, change = 0, sent = 0;

                transactions.Where(tx => tx.WalletType == WalletType.Receive).ToList().ForEach(x =>
                {
                    foreach (var v in x.Vout)
                    {
                        received += Util.MessageAmount(v, scan);
                    }
                });

                transactions.Where(tx => tx.WalletType == WalletType.Send).ToList().ForEach(x =>
                {
                    fee += Util.MessageAmount(x.Vout[0], scan);
                    payment += Util.MessageAmount(x.Vout[1], scan);
                    change = Util.MessageAmount(x.Vout[2], scan);

                    sent += received - change;                    
                });

                total = change;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }

            return total;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<IEnumerable<BalanceSheet>> History(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var balanceSheets = new List<BalanceSheet>();
            List<WalletTransaction> walletTransactions;

            var session = Session(sessionId);

            walletTransactions = session.Database.Query<WalletTransaction>()
                .OrderBy(x => x.DateTime)
                .ToList();
            if (walletTransactions?.Any() != true)
            {
                return TaskResult<IEnumerable<BalanceSheet>>.CreateSuccess(balanceSheets);
            }

            ulong received = 0, sent = 0;

            var (spend, scan) = Unlock(session.SessionId);

            walletTransactions.Where(tx => tx.WalletType == WalletType.Receive).ToList().ForEach(x =>
            {
                foreach (var v in x.Vout)
                {
                    x.Payment += Util.MessageAmount(v, scan);

                    if (string.IsNullOrEmpty(x.Memo))
                    {
                        x.Memo = Util.MessageMemo(v, scan);
                    }
                }

                balanceSheets.Add(new BalanceSheet
                {
                    DateTime = x.DateTime,
                    CoinType = session.SessionType == SessionType.Coin ? CoinType.Coin.ToString() : CoinType.Coinstake.ToString(),
                    Memo = x.Memo,
                    MoneyIn = x.Payment.DivWithNaT().ToString("F9"),
                    Balance = x.Payment.DivWithNaT().ToString("F9")
                });

                received += x.Payment;
            });

            walletTransactions.Where(tx => tx.WalletType == WalletType.Send).ToList().ForEach(x =>
            {
                x.Fee = Util.MessageAmount(x.Vout[0], scan);
                x.Payment = Util.MessageAmount(x.Vout[1], scan);
                x.Change = Util.MessageAmount(x.Vout[2], scan);

                ulong fee = x.Fee = x.Vout[0].T == CoinType.fee ? x.Fee : 0;

                sent = received - x.Change - fee;

                received = x.Change;

                balanceSheets.Add(new BalanceSheet
                {
                    DateTime = x.DateTime,
                    CoinType = session.SessionType == SessionType.Coin ? CoinType.Coin.ToString() : CoinType.Coinstake.ToString(),
                    Memo = x.Memo,
                    MoneyOut = $"-{sent.DivWithNaT():F9}",
                    Fee = $"-{x.Fee.DivWithNaT():F9}",
                    Balance = x.Change.DivWithNaT().ToString("F9")
                });
            });

            return TaskResult<IEnumerable<BalanceSheet>>.CreateSuccess(balanceSheets);
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

            var keySet = session.Database.Query<KeySet>().ToList().Last();

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

            if (session.WalletTransaction == null)
            {
                session.WalletTransaction = new WalletTransaction();
            }

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
                _logger.LogError(ex.Message);
            }

            return keySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public (PubKey, StealthPayment) MakeStealthPayment(string address)
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

            try
            {
                var session = Session(sessionId);

                var walletTransactions = session.Database.Query<WalletTransaction>().ToList();
                if (walletTransactions.Any())
                {
                    var walletTransaction = walletTransactions.FirstOrDefault(x => x.TxId.SequenceEqual(paymentId.HexToByte()) && x.WalletType == WalletType.Receive);
                    if (walletTransaction != null)
                    {
                        var vout = TaskResult<WalletTransaction>.CreateFailure(new Exception($"Transaction with paymentId: {paymentId} already exists"));
                        SetLastError(session, vout);
                        return vout;
                    }
                }

                var baseAddress = _client.GetBaseAddress();
                var path = string.Format(_apiGatewaySection.GetSection(RestCall.Routing).GetValue<string>(RestCall.GetTransactionId.ToString()), paymentId);

                var vouts = await _client.GetRangeAsync<Vout>(baseAddress, path, new System.Threading.CancellationToken());
                if (vouts.Any() != true)
                {
                    var vout = TaskResult<WalletTransaction>.CreateFailure(new Exception($"Failed to find transaction with paymentId: {paymentId}"));
                    SetLastError(session, vout);
                    return vout;
                }

                var (spend, scan) = Unlock(session.SessionId);
                var vOutList = new List<Vout>();

                foreach (var v in vouts)
                {
                    var uncover = spend.Uncover(scan, new PubKey(v.E));
                    if (uncover.PubKey.ToBytes().SequenceEqual(v.P))
                    {
                        vOutList.Add(v);
                    }
                }

                if (vOutList.Any() != true)
                    return TaskResult<WalletTransaction>.CreateFailure("vOutList empty");

                session.WalletTransaction = new WalletTransaction
                {
                    SenderAddress = session.WalletTransaction.SenderAddress,
                    DateTime = DateTime.UtcNow,
                    Vout = vOutList.ToArray(),
                    TxId = paymentId.HexToByte(),
                    WalletType = WalletType.Receive
                };

                SessionAddOrUpdate(session);

                var saved = Save(session.SessionId, session.WalletTransaction);
                if (!saved.Success)
                {
                    SetLastError(session, saved);
                    return TaskResult<WalletTransaction>.CreateFailure(saved);
                }

                return TaskResult<WalletTransaction>.CreateSuccess(session.WalletTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        public TaskResult<WalletTransaction> CreatePayment(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            try
            {
                var session = Session(sessionId);
                session.LastError = null;

                var balance = AvailableBalance(session.SessionId);
                if (!balance.Success)
                {
                    SetLastError(session, balance);
                    return TaskResult<WalletTransaction>.CreateFailure(balance);
                }

                var calculated = CalculateChange(session.SessionId);
                if (!calculated.Success)
                {
                    SetLastError(session, calculated);
                    return TaskResult<WalletTransaction>.CreateFailure(calculated);
                }

                var transaction = CreateTransaction(session.SessionId);
                if (!transaction.Success)
                {
                    SetLastError(session, transaction);
                    return TaskResult<WalletTransaction>.CreateFailure(transaction);
                }

                var saved = Save(session.SessionId, session.WalletTransaction);
                if (!saved.Success)
                {
                    SetLastError(session, saved);
                    return TaskResult<WalletTransaction>.CreateFailure(transaction);
                }

                return TaskResult<WalletTransaction>.CreateSuccess(session.WalletTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task<TaskResult<bool>> Send(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = Session(sessionId);
            session.LastError = null;

            var transaction = Transaction(session.SessionId);

            var baseAddress = _client.GetBaseAddress();
            var path = _apiGatewaySection.GetSection(RestCall.Routing).GetValue<string>(RestCall.GetTransactionId.ToString());

            var posted = await _client.PostAsync(transaction, baseAddress, path, new System.Threading.CancellationToken());
            if (posted == null)
            {
                var fail = TaskResult<bool>.CreateFailure(new Exception($"Unable to send transaction with paymentId: {transaction.TxnId.ByteToHex()}"));
                SetLastError(session, fail);
                RollBackOne(session.SessionId);
                return fail;
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="obj"></param>
        private void SetLastError<T>(Session session, TaskResult<T> obj)
        {
            if (obj.Exception == null)
            {
                session.LastError = obj.NonSuccessMessage;
                _logger.LogError($"{obj.NonSuccessMessage.message}");
            }
            else
            {
                session.LastError = JObject.FromObject(new
                {
                    success = false,
                    message = obj.Exception.Message
                });

                _logger.LogError(obj.Exception.Message);
            }

            SessionAddOrUpdate(session);
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

                spend = masterKey.Derive(new KeyPath($"{HDPath}0")).PrivateKey;
                scan = masterKey.Derive(new KeyPath($"{HDPath}1")).PrivateKey;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
            }

            return (spend, scan);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<bool> Save<T>(Guid sessionId, T data)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            try
            {
                var session = Session(sessionId);
                session.Database.Insert(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private TaskResult<bool> RollBackOne(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            try
            {
                var session = Session(sessionId);

                var transaction = session.Database.Query<Transaction>().Where(s => s.Id == session.SessionId).FirstOrDefault();
                if (transaction != null)
                {
                    session.Database.Delete<Transaction>(new LiteDB.BsonValue(transaction.Id));
                }

                var walletTransaction = session.Database.Query<WalletTransaction>().Where(s => s.Id == session.SessionId).FirstOrDefault();
                if (walletTransaction != null)
                {
                    session.Database.Delete<WalletTransaction>(new LiteDB.BsonValue(walletTransaction.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }
    }
}