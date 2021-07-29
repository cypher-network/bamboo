// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using BAMWallet.Model;
using Dawn;
using MessagePack;
using Microsoft.AspNetCore.Http;
namespace BAMWallet.Rpc.Controllers
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

        [HttpPost("address", Name = "Address")]
        public IActionResult Addresses([FromBody] Credentials credentials)
        {
            Guard.Argument(credentials, nameof(credentials)).NotNull();

            var session = new Session(credentials.Identifier.ToSecureString(), credentials.Passphrase.ToSecureString());

            var request = _walletService.Address(session);
            if (!request.Success)
                return new BadRequestObjectResult(request.NonSuccessMessage);

            if (request.Result.Any() != true)
                return new NotFoundResult();

            return new OkObjectResult(request.Result);
        }

        [HttpPost("balance", Name = "Balance")]
        public IActionResult Balance([FromBody] Credentials credentials)
        {
            Guard.Argument(credentials, nameof(credentials)).NotNull();

            var session = new Session(credentials.Identifier.ToSecureString(), credentials.Passphrase.ToSecureString());

            var total = _walletService.History(session);
            if (!total.Success)
                return new BadRequestObjectResult(total.NonSuccessMessage);

            return new OkObjectResult($"{total.Result.Last()}");
        }

        [HttpGet("create", Name = "Create")]
        public async Task<IActionResult> Create(string seed = null, string passphrase = null)
        {
            string[] mnemonicDefault = await _walletService.CreateSeed(Language.English, WordCount.TwentyFour);
            string[] passphraseDefault = await _walletService.CreateSeed(Language.English, WordCount.Twelve);
            string joinMmnemonic = string.Join(" ", seed ?? string.Join(' ', mnemonicDefault));
            string joinPassphrase = string.Join(" ", passphrase ?? string.Join(' ', passphraseDefault));
            string id = _walletService.CreateWallet(joinMmnemonic.ToSecureString(), joinPassphrase.ToSecureString());
            var session = new Session(id.ToSecureString(), joinPassphrase.ToSecureString());

            var request = _walletService.Address(session);
            if (!request.Success)
                return new BadRequestObjectResult(request.NonSuccessMessage);

            if (request.Result.Any() != true)
                return new NotFoundResult();

            return new OkObjectResult(new
            {
                path = Util.WalletPath(id),
                identifier = id,
                seed = joinMmnemonic,
                passphrase = joinPassphrase,
                address = request.Result
            });
        }

        [HttpGet("seed", Name = "CreateSeed")]
        public async Task<IActionResult> CreateSeed(Language language = Language.English,
            WordCount mnemonicWordCount = WordCount.TwentyFour,
            WordCount passphraseWordCount = WordCount.Twelve)
        {
            var seed = await _walletService.CreateSeed(language, mnemonicWordCount);
            var passphrase = await _walletService.CreateSeed(language, passphraseWordCount);

            return new ObjectResult(new
            {
                seed,
                passphrase
            });
        }

        // TODO: does this method expose too much (full path)? is this even required?
        [HttpGet("list", Name = "List")]
        public IActionResult List()
        {
            var request = _walletService.WalletList();

            if (!request.Success)
                return new BadRequestObjectResult(request.NonSuccessMessage);

            if (request.Result.Any() != true)
                return new NotFoundResult();

            return new OkObjectResult(request.Result);
        }

        [HttpPost("history", Name = "History")]
        public IActionResult History([FromBody] Credentials credentials)
        {
            Guard.Argument(credentials, nameof(credentials)).NotNull();

            var session = new Session(credentials.Identifier.ToSecureString(), credentials.Passphrase.ToSecureString());

            var request = _walletService.History(session);
            if (!request.Success)
                return new BadRequestObjectResult(request.NonSuccessMessage);

            return new OkObjectResult(request.Result);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("transaction", Name = "CreateTransaction")]
        public IActionResult CreateTransaction([FromBody] byte[] data)
        {
            var payment = MessagePackSerializer.Deserialize<SendPayment>(data);
            var session = new Session(payment.Credentials.Identifier.ToSecureString(), payment.Credentials.Passphrase.ToSecureString());

            session.SessionType = payment.SessionType;
            session.WalletTransaction = new WalletTransaction
            {
                Delay = 5,
                Payment = payment.Amount,
                Reward = payment.SessionType == SessionType.Coinstake ? payment.Reward : 0,
                Memo = payment.Memo,
                RecipientAddress = payment.Address,
                WalletType = WalletType.Send
            };


            var walletTransaction = _walletService.CreateTransaction(session);
            if (!walletTransaction.Success) return new StatusCodeResult(StatusCodes.Status404NotFound);

            var transaction = _walletService.GetTransaction(session);
            if (transaction != null)
            {
                return new ObjectResult(new { messagepack = transaction.Serialize() });
            }

            return new StatusCodeResult(StatusCodes.Status404NotFound);
        }

        [HttpPost("receive", Name = "Receive")]
        public async Task<IActionResult> Receive([FromBody] Receive receive)
        {
            Guard.Argument(receive.Identifier, nameof(receive.Identifier)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(receive.Passphrase, nameof(receive.Passphrase)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(receive.PaymentId, nameof(receive.PaymentId)).NotNull().NotEmpty().NotWhiteSpace();

            var session = new Session(receive.Identifier.ToSecureString(), receive.Passphrase.ToSecureString());

            var request = await _walletService.ReceivePayment(session, receive.PaymentId);
            if (!request.Success)
                return new BadRequestObjectResult(request.NonSuccessMessage);

            var balanceSheet = _walletService.History(session).Result.Last();
            return new OkObjectResult(new
            {
                memo = balanceSheet.Memo,
                received = balanceSheet.MoneyIn,
                balance = $"{balanceSheet.Balance}"
            });
        }

        [HttpPost("spend", Name = "Spend")]
        public async Task<IActionResult> Spend([FromBody] Spend spend)
        {
            Guard.Argument(spend.Identifier, nameof(spend.Identifier)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(spend.Passphrase, nameof(spend.Passphrase)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(spend.Address, nameof(spend.Address)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(spend.Amount, nameof(spend.Amount)).Positive();


            var session = new Session(spend.Identifier.ToSecureString(), spend.Passphrase.ToSecureString());

            session.SessionType = SessionType.Coin;
            session.WalletTransaction = new WalletTransaction
            {
                Memo = spend.Memo,
                Payment = spend.Amount.ConvertToUInt64(),
                RecipientAddress = spend.Address,
                WalletType = WalletType.Send
            };

            var createPayment = _walletService.CreateTransaction(session);
            if (!createPayment.Success)
                return new BadRequestObjectResult(createPayment.NonSuccessMessage);

            var send = await _walletService.Send(session);
            if (!send.Success)
                return new BadRequestObjectResult(send.NonSuccessMessage);

            var balance = _walletService.History(session);
            var walletTx = _walletService.GetTransaction(session);

            return new OkObjectResult(new
            {
                balance = $"{balance.Result.Last().Balance}",
                paymentId = walletTx?.TxnId.ByteToHex()
            });
        }
    }
}