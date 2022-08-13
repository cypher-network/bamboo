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
using BAMWallet.HD;
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

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                if (activeSession == null) return;
                _console.ForegroundColor = ConsoleColor.Green;
                _console.WriteLine("Please enter in an option:");
                _console.ForegroundColor = ConsoleColor.Gray;
                _console.WriteLine("All [4] by default without Not Found transactions.");
                _console.ForegroundColor = ConsoleColor.Yellow;
                _console.WriteLine("Waiting Confirmation  [1]\n" + "Confirmed             [2]\n" + "Not Found             [3]\n" + "All                   [4]");
                _console.ForegroundColor = ConsoleColor.White;
                var status = Prompt.GetInt("Option:", (int?)WalletTransactionState.All, ConsoleColor.Magenta);
                await Spinner.StartAsync("Looking up history ...", async spinner =>
                {
                    BalanceSheet[] orderedBalanceSheets;
                    await _commandReceiver.SyncWallet(activeSession);
                    if (status == (int)WalletTransactionState.NotFound)
                    {
                        var (balances, message) = _commandReceiver.NotFoundTransactions(activeSession);
                        if (balances is null)
                        {
                            NoBalances(spinner, message);
                            return Task.CompletedTask;
                        }

                        var balance = balances as IList<BalanceSheet>;
                        if (!balance.Any())
                        {
                            NoTxn();
                            return Task.CompletedTask;
                        }

                        orderedBalanceSheets = balance.ToArray();
                    }
                    else
                    {
                        var (balances, message) = _commandReceiver.History(activeSession);
                        if (balances is null)
                        {
                            NoBalances(spinner, message);
                            return Task.CompletedTask;
                        }

                        var balance = balances as IList<BalanceSheet>;
                        if (!balance.Any())
                        {
                            NoTxn();
                            return Task.CompletedTask;
                        }

                        orderedBalanceSheets = status != (int)WalletTransactionState.All
                            ? balance.Where(x => x.State == (WalletTransactionState)status).ToArray()
                            : balance.ToArray();
                    }

                    DrawTable(orderedBalanceSheets);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                NoTxn(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spinner"></param>
        /// <param name="message"></param>
        private void NoBalances(Spinner spinner, string message)
        {
            _console.ForegroundColor = ConsoleColor.Red;
            spinner.Fail($"{message}");
            _console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderedBalanceSheets"></param>
        private void DrawTable(BalanceSheet[] orderedBalanceSheets)
        {
            var table = new ConsoleTable("Date Time", "Transaction Id", "Memo", "Money In", "Money Out",
                "Coinbase", "Balance", "Status", "Locked");
            foreach (var sheet in orderedBalanceSheets.OrderBy(x => x.Date))
            {
                table.AddRow(sheet.Date.ToString("dd-MM-yyyy HH:mm:ss"), sheet.TxId, sheet.Memo, sheet.MoneyIn, sheet.MoneyOut, sheet.Reward,
                    sheet.Balance, sheet.State.ToString(), sheet.IsLocked.ToString());
            }

            table.Configure(o => o.NumberAlignment = Alignment.Right);
            _console.WriteLine($"\n{table}");
            _console.WriteLine("\n");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        private void NoTxn(Exception ex = null)
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine($"\nWallet has no transactions.\n");
            if (ex == null) return;
            _console.WriteLine(ex.Message);
            _console.ForegroundColor = ConsoleColor.White;
        }
    }
}
