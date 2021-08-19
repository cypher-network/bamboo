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
using BAMWallet.HD;
using BAMWallet.Helper;
using BAMWallet.Model;
using Cli.Commands.Common;
using ConsoleTables;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("history", "Show my transactions")]
    public class WalletTxHistoryCommand : Command
    {
        public WalletTxHistoryCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletTxHistoryCommand), serviceProvider, true)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            try
            {
                if(activeSession != null)
                {
                    Spinner.StartAsync("Looking up history ...", spinner =>
                    {
                        var balanceResult = _walletService.History(session);
                        if (balanceResult.Item1 is null)
                        {
                            _console.ForegroundColor = ConsoleColor.Red;
                            spinner.Fail($"{balanceResult.Item2}");
                            _console.ForegroundColor = ConsoleColor.White;
                            return Task.CompletedTask;
                        }
                        else
                        {
                            var balance = (balanceResult.Item1 as IOrderedEnumerable<BalanceSheet>);

                            if (!balance.Any())
                            {
                                NoTxn();
                                return Task.CompletedTask;
                            }

                            var table = new ConsoleTable(
                                "DateTime",
                                "Payment Id",
                                "Memo",
                                "Money In",
                                "Money Out",
                                "Reward",
                                "Balance",
                                "Verified",
                                "Locked");

                            foreach (var sheet in balance)
                            {
                                table.AddRow(
                                    sheet.Date,
                                    sheet.TxId,
                                    sheet.Memo,
                                    sheet.MoneyIn,
                                    sheet.MoneyOut,
                                    sheet.Reward,
                                    sheet.Balance,
                                    sheet.IsVerified.ToString(),
                                    sheet.IsLocked.ToString());
                            }

                            table.Configure(o => o.NumberAlignment = Alignment.Right);

                            _console.WriteLine($"\n{table}");

                            return Task.CompletedTask;
                        }
                    });
                }
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
            {
                _console.WriteLine(ex.Message);
                _console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
