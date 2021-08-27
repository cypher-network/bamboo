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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("receive", "Receive a payment")]
    public class WalletReceivePaymentCommand : Command
    {
        private readonly ILogger _logger;
        private Spinner spinner;

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletReceivePaymentCommand), serviceProvider, true)
        {
            _logger = serviceProvider.GetService<ILogger<WalletReceivePaymentCommand>>();
        }

        public override void Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                var paymentId = Prompt.GetString("PAYMENTID:", null, ConsoleColor.Green);
                if (!string.IsNullOrEmpty(paymentId))
                {
                    Spinner.StartAsync("Receiving payment...", spinner =>
                   {
                       this.spinner = spinner;
                       try
                       {
                           var receivePaymentResult = _walletService.ReceivePayment(activeSession, paymentId);
                           if (receivePaymentResult.Item1 is null)
                           {
                               spinner.Fail(receivePaymentResult.Item2);
                           }
                           else
                           {
                               var balanceResult = _walletService.History(activeSession);
                               if (balanceResult.Item1 is null)
                               {
                                   spinner.Fail(balanceResult.Item2);
                               }
                               else
                               {
                                   var lastSheet = (balanceResult.Item1 as IOrderedEnumerable<BalanceSheet>).Last();
                                   spinner.Succeed($"Memo: {lastSheet.Memo}  Received: {lastSheet.MoneyIn}  Available Balance: {lastSheet.Balance}");
                               }
                           }
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
}

