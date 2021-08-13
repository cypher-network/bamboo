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

using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

using BAMWallet.HD;
using BAMWallet.Helper;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("receive", "Receive a payment")]
    public class WalletReceivePaymentCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly ILogger _logger;

        private Spinner spinner;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletReceivePaymentCommand), serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _logger = serviceProvider.GetService<ILogger<WalletReceivePaymentCommand>>();
        }

        public override void Execute()
        {
            this.Login();
            using var KeepLoginState = new RAIIGuard(Command.FreezeTimer, Command.UnfreezeTimer);
            var paymentId = Prompt.GetString("PAYMENTID:", null, ConsoleColor.Green);
            if (!string.IsNullOrEmpty(paymentId))
            {
                Spinner.StartAsync("Receiving payment...",  spinner =>
                {
                    this.spinner = spinner;
                    try
                    {
                        var session = ActiveSession;
                        _walletService.ReceivePayment(session, paymentId);
                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                        }

                        var balance = _walletService.History(session).Result.Last();
                        spinner.Succeed(
                            $"Memo: {balance.Memo}  Received: {balance.MoneyIn}  Available Balance: {balance.Balance}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                        throw;
                    }
                    return Task.CompletedTask;
                }, Patterns.Toggle3);
            }
        }
    }
}

