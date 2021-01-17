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