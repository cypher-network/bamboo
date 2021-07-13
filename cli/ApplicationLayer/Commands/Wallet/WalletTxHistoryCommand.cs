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

using McMaster.Extensions.CommandLineUtils;

using ConsoleTables;

using BAMWallet.HD;
using Kurukuru;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "history" }, "Show my transactions")]
    public class WalletTxHistoryCommand : Command
    {
        private readonly IConsole _console;
        private readonly IWalletService _walletService;

        public WalletTxHistoryCommand(IServiceProvider serviceProvider)
        {
            _console = serviceProvider.GetService<IConsole>();
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override async Task Execute()
        {
            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            try
            {
                var session = _walletService.SessionAddOrUpdate(new Session(identifier, passphrase));

                await Spinner.StartAsync("Looking up history ...", spinner =>
               {
                   var request = _walletService.History(session.SessionId);

                   if (!request.Success)
                   {
                       _console.ForegroundColor = ConsoleColor.Red;
                       _console.WriteLine("\nWallet history request failed.");

                       if (request.NonSuccessMessage != null)
                       {
                           _console.WriteLine($"{request.NonSuccessMessage}");
                       }
                       _console.ForegroundColor = ConsoleColor.White;
                       return Task.CompletedTask;
                   }

                   if (!request.Result.Any())
                   {
                       NoTxn();
                       return Task.CompletedTask;
                   }

                   var table = new ConsoleTable("DateTime", "Payment Id", "Memo", "Money In", "Money Out", "Reward", "Balance");

                   foreach (var sheet in request.Result)
                   {
                       table.AddRow(sheet.Date, sheet.TxId, sheet.Memo, sheet.MoneyIn, sheet.MoneyOut, sheet.Reward,
                           sheet.Balance);
                   }

                   table.Configure(o => o.NumberAlignment = Alignment.Right);

                   _console.WriteLine($"\n{table.ToString()}");

                   return Task.CompletedTask;
               });
            }
            catch (Exception ex)
            {
                NoTxn(ex);
            }
        }

        private void NoTxn(Exception ex = null)
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine($"\nWallet has no transactions.\n");
            if (ex != null)
                _console.WriteLine(ex.Message);
            _console.ForegroundColor = ConsoleColor.White;
        }
    }
}
