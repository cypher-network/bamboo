// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

using BAMWallet.Extentions;
using BAMWallet.HD;
using BAMWallet.Helper;
using BAMWallet.Model;

namespace BAMWallet.Controllers
{
    [Route("api/wallet")]
    [ApiController]
    public class WalletController
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpPost("address", Name = "GetWalletAddresses")]
        public IActionResult GetWalletAddresses([FromBody] Credentials credentials)
        {
            var session = _walletService.SessionAddOrUpdate(new Session(credentials.Identifier.ToSecureString(),
                credentials.Passphrase.ToSecureString()));
            var addresses = _walletService.Addresses(session.SessionId);

            if (addresses.Any())
                return new OkObjectResult(addresses);

            return new NotFoundResult();
        }

        [HttpGet("create", Name = "CreateWallet")]
        public async Task<IActionResult> CreateWallet()
        {
            string[] mnemonicDefault = await _walletService.CreateMnemonic(NBitcoin.Language.English, NBitcoin.WordCount.TwentyFour);
            string[] passphraseDefault = await _walletService.CreateMnemonic(NBitcoin.Language.English, NBitcoin.WordCount.Twelve);
            string joinMmnemonic = string.Join(" ", mnemonicDefault);
            string joinPassphrase = string.Join(" ", passphraseDefault);
            string id = _walletService.CreateWallet(joinMmnemonic.ToSecureString(), joinPassphrase.ToSecureString());
            var session = _walletService.SessionAddOrUpdate(new Session(id.ToSecureString(),
                joinPassphrase.ToSecureString()));
            var address = _walletService.Addresses(session.SessionId).First();
            return new OkObjectResult(new
            {
                path = Util.WalletPath(id),
                identifier = id,
                mnemonic = joinMmnemonic,
                passphrase = joinPassphrase,
                address
            });
        }

        // TODO: issue with AddKeySet - tries to open the db multiple times
        //[HttpPost("createAddress", Name = "CreateWalletAddress")]
        //public async Task<IActionResult> CreateWalletAddress([FromBody] Credentials credentials)
        //{
        //    var session = _walletService.SessionAddOrUpdate(new Session(credentials.Identifier.ToSecureString(),
        //        credentials.Passphrase.ToSecureString()));
        //    _walletService.AddKeySet(session.SessionId);
        //    var last = _walletService.LastKeySet(session.SessionId);
        //    return new OkObjectResult(last.StealthAddress);
        //}

        [HttpPost("transaction", Name = "CreateTransacrtion")]
        public IActionResult CreateTransaction([FromBody] byte[] sendPayment)
        {
            var payment = Util.DeserializeProto<SendPayment>(sendPayment);
            var session = _walletService.SessionAddOrUpdate(new Session(payment.Credentials.Identifier.ToSecureString(), payment.Credentials.Passphrase.ToSecureString())
            {
                SessionType = payment.SessionType,
                WalletTransaction = new WalletTransaction
                {
                    Fee = payment.SessionType == SessionType.Coin ? payment.Fee : 0,
                    Payment = payment.Amount,
                    Reward = payment.SessionType == SessionType.Coinstake ? payment.Fee : 0,
                    Memo = payment.Memo,
                    RecipientAddress = payment.Address
                }
            });

            _walletService.CreatePayment(session.SessionId);

            var transaction = _walletService.Transaction(session.SessionId);
            var txByteArray = Util.SerializeProto(transaction);

            return new ObjectResult(new { protobuf = txByteArray });
        }
    }
}