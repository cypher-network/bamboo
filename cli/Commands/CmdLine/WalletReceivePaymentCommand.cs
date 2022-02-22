// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
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

        public WalletReceivePaymentCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletReceivePaymentCommand), serviceProvider, true)
        {
            _logger = serviceProvider.GetService<ILogger<WalletReceivePaymentCommand>>();
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                var paymentId = Prompt.GetString("TxID:", null, ConsoleColor.Green);
                if (!string.IsNullOrEmpty(paymentId))
                {
                    await Spinner.StartAsync("Receiving payment...", spinner =>
                   {
                       try
                       {
                           var (receive, errorReceive) = _commandReceiver.ReceivePayment(activeSession, paymentId);
                           if (receive is null)
                           {
                               spinner.Fail(errorReceive);
                           }
                           else
                           {
                               var (balances, errorBalances) = _commandReceiver.History(activeSession);
                               if (balances is not null)
                               {
                                   if (balances is IList<BalanceSheet> balanceSheet)
                                   {
                                       var transactions = balanceSheet.Where(x => x.TxId == balanceSheet.Last().TxId);
                                       var received = transactions.Sum(x => Convert.ToDecimal(x.MoneyIn));
                                       var balance = transactions.Sum(x => Convert.ToDecimal(x.Balance));
                                       spinner.Succeed(
                                           $"Memo: {transactions.First().Memo} Received: [{received:F9}] Available Balance: [{balance:F9}]");
                                   }
                               }
                               else
                               {
                                   spinner.Fail(errorBalances);
                               }
                           }
                       }
                       catch (Exception ex)
                       {
                           _logger.LogError("Message: {@msg}\n Stack: {@trace}", ex.Message, ex.StackTrace);
                           throw;
                       }
                       return Task.CompletedTask;
                   }, Patterns.Toggle3);
                }
            }
        }
    }
}

