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
using BAMWallet.Model;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Cli.Commands.Common;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("balance", "Get your wallet balance")]
    public class WalletBalanceCommand : Command
    {
        private Spinner _spinner;
        public WalletBalanceCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletBalanceCommand), serviceProvider, true)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                Spinner.StartAsync("Checking balance ...", spinner =>
                {
                    _spinner = spinner;
                    var balanceResult = _commandReceiver.History(activeSession);
                    if (balanceResult.Item1 is null)
                    {
                        spinner.Fail(balanceResult.Item2);
                    }
                    else
                    {
                        var lastSheet = (balanceResult.Item1 as IOrderedEnumerable<BalanceSheet>).Last();
                        _console.ForegroundColor = ConsoleColor.Green;
                        _console.WriteLine($"\n Balance: {lastSheet.Balance}");
                        _console.ForegroundColor = ConsoleColor.White;
                        spinner.Succeed();
                    }
                    return Task.CompletedTask;
                });
            }
        }
    }
}
