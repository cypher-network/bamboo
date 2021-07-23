// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using BAMWallet.HD;
using BAMWallet.Extensions;
namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("receive", "Receive a payment")]
    public class WalletReceivePaymentCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly ILogger _logger;

        private Spinner spinner;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider): base(typeof(WalletReceivePaymentCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(WalletReceivePaymentCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description))
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _logger = serviceProvider.GetService<ILogger<WalletReceivePaymentCommand>>();
        }

        public override async Task Execute()
        {
            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var paymentId = Prompt.GetString("PAYMENTID:", null, ConsoleColor.Green);
            if (!string.IsNullOrEmpty(paymentId))
            {
                await Spinner.StartAsync("Receiving payment...", async spinner =>
                {
                    this.spinner = spinner;
                    try
                    {
                        var session = _walletService.SessionAddOrUpdate(new Session(identifier, passphrase));
                        await _walletService.ReceivePayment(session.SessionId, paymentId);
                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                            return;
                        }

                        var balance = _walletService.History(session.SessionId).Result.Last();
                        spinner.Succeed(
                            $"Memo: {balance.Memo}  Received: {balance.MoneyIn}  Available Balance: {balance.Balance}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                        throw;
                    }
                }, Patterns.Toggle3);
            }
        }
    }
}

