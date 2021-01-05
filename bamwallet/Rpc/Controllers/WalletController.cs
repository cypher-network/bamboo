// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("bampos", Name = "CreateTransacrtion")]
        public IActionResult CreateTransaction([FromBody] byte[] sendPayment)
        {
            var payment = Util.DeserializeProto<SendPayment>(sendPayment);
            var session = _walletService.SessionAddOrUpdate(new Session(payment.Credentials.Identifier.ToSecureString(), payment.Credentials.Passphrase.ToSecureString())
            {
                Amount = payment.Amount.ConvertToUInt64(),
                Memo = payment.Memo,
                RecipientAddress = payment.Address,
                SessionType = payment.SessionType
            });

            var bal = _walletService.AvailableBalance(session.SessionId);
            if (!bal.Success)
            {
                return new OkObjectResult(null);
            }

            var walletTx = new WalletTransaction
            {
                Balance = bal.Result,
                Payment = payment.Amount.ConvertToUInt64()
            };

            var tx = _walletService.CreateTransaction(session, walletTx);

            return new OkObjectResult(tx.Result);
        }
    }
}