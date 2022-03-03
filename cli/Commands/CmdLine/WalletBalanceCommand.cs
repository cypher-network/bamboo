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
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Cli.Commands.Common;
using ConsoleTables;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("balance", "Get your wallet balance")]
    public class WalletBalanceCommand : Command
    {
        public WalletBalanceCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletBalanceCommand), serviceProvider, true)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                await Spinner.StartAsync("Checking confirmed balance(s) ...", spinner =>
                {
                    var balances = _commandReceiver.GetBalances(activeSession);
                    if (balances.Length == 0)
                    {
                        spinner.Fail("Nothing to see.");
                    }
                    else
                    {
                        var table = new ConsoleTable("Payments", "Coinstake", "Coinbase", "Change", "Balance");
                        var payment = balances.Where(x => x.Commitment.T == CoinType.Payment).Sum(x => x.Total.DivWithGYin());
                        var coinstake = balances.Where(x => x.Commitment.T == CoinType.Coinstake).Sum(x => x.Total.DivWithGYin());
                        var coinbase = balances.Where(x => x.Commitment.T == CoinType.Coinbase).Sum(x => x.Total.DivWithGYin());
                        var change = balances.Where(x => x.Commitment.T == CoinType.Change).Sum(x => x.Total.DivWithGYin());

                        var balance = payment + coinstake + coinbase + change;

                        table.AddRow($"{payment:F9}", $"{coinstake:F9}", $"{coinbase:F9}", $"{change:F9}", $"{balance:F9}");

                        table.Configure(o => o.NumberAlignment = Alignment.Right);
                        _console.WriteLine($"\n{table}");
                        _console.WriteLine("\n");
                    }
                    return Task.CompletedTask;
                });
            }
        }
    }
}
