// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

using NBitcoin;

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

        [HttpPost("address", Name = "GetAddresses")]
        public IActionResult GetAddresses([FromBody] Credentials credentials)
        {
            var session = _walletService.SessionAddOrUpdate(new Session(credentials.Identifier.ToSecureString(),
                credentials.Passphrase.ToSecureString()));

            var request = _walletService.Addresses(session.SessionId);
            if (request.Success)
                if (request.Result.Any())
                    return new OkObjectResult(request.Result);
                else
                    return new NotFoundResult();

            return new BadRequestObjectResult(request.NonSuccessMessage);
        }

        [HttpGet("create", Name = "Create")]
        public async Task<IActionResult> Create(string mnemonic = null, string passphrase = null)
        {
            string[] mnemonicDefault = await _walletService.CreateMnemonic(Language.English, WordCount.TwentyFour);
            string[] passphraseDefault = await _walletService.CreateMnemonic(Language.English, WordCount.Twelve);
            string joinMmnemonic = string.Join(" ", mnemonic ?? string.Join(' ', mnemonicDefault));
            string joinPassphrase = string.Join(" ", passphrase ?? string.Join(' ', passphraseDefault));
            string id = _walletService.CreateWallet(joinMmnemonic.ToSecureString(), joinPassphrase.ToSecureString());
            var session = _walletService.SessionAddOrUpdate(new Session(id.ToSecureString(),
                joinPassphrase.ToSecureString()));

            var request = _walletService.Addresses(session.SessionId);
            if (request.Success)
                if (request.Result.Any())
                    return new OkObjectResult(new
                    {
                        path = Util.WalletPath(id),
                        identifier = id,
                        mnemonic = joinMmnemonic,
                        passphrase = joinPassphrase,
                        address = request.Result
                    });
                else
                    return new NotFoundResult();

            return new BadRequestObjectResult(request.NonSuccessMessage);
        }

        [HttpPost("balance", Name = "GetBalance")]
        public IActionResult GetBalance([FromBody] Credentials credentials)
        {
            var session = _walletService.SessionAddOrUpdate(new Session(credentials.Identifier.ToSecureString(),
                credentials.Passphrase.ToSecureString()));

            var total = _walletService.AvailableBalance(session.SessionId);
            if (total.Success)
                return new OkObjectResult($"{total.Result.DivWithNaT():F9}");

            return new BadRequestObjectResult(total.NonSuccessMessage);
        }

        [HttpGet("mnemonic", Name = "CreateMnemonic")]
        public async Task<IActionResult> CreateMnemonic(Language language = Language.English,
            WordCount mnemonicWordCount = WordCount.TwentyFour,
            WordCount passphraseWordCount = WordCount.Twelve)
        {
            var mnemonic = await _walletService.CreateMnemonic(language, mnemonicWordCount);
            var passphrase = await _walletService.CreateMnemonic(language, passphraseWordCount);

            return new ObjectResult(new
            {
                mnemonic,
                passphrase
            });
        }

        // TODO: does this method expose too much (full path)? is it event required?
        [HttpGet("list", Name = "GetList")]
        public IActionResult GetList()
        {
            var request = _walletService.WalletList();
            if (request.Success)
                if (request.Result.Any())
                    return new OkObjectResult(request.Result);
                else
                    return new NotFoundResult();

            return new BadRequestObjectResult(request.NonSuccessMessage);
        }

        [HttpPost("history", Name = "GetHistory")]
        public IActionResult GetHistory([FromBody] Credentials credentials)
        {
            var session = _walletService.SessionAddOrUpdate(new Session(credentials.Identifier.ToSecureString(),
                credentials.Passphrase.ToSecureString()));

            var request = _walletService.History(session.SessionId);
            if (request.Success)
                return new OkObjectResult(request.Result);

            return new BadRequestObjectResult(request.NonSuccessMessage);
        }

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