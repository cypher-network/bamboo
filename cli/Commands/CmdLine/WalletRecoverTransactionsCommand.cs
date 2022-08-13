// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Kurukuru;
using Cli.Commands.Common;
using BAMWallet.HD;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("recover", "Recover wallet transactions")]
    public class WalletRecoverTransactionsCommand : Command
    {
        public WalletRecoverTransactionsCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletRecoverTransactionsCommand), serviceProvider, true)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession == null) return;
            var yesno = Prompt.GetYesNo("Restoring your wallet is an expensive operation that requires downloading large amounts of data.\n" +
                                        "Please be specific when entering the block height where you know when the first transaction was received.\n" +
                                        "If you don't know the specific block height then please ask for instructions in general: https://discord.gg/6DT3yFhXCB \n" +
                                        "Backup your wallet before starting or if restoring to a clean wallet then no backup is required.\n" +
                                        "To continue, enter y or N to exit.", false, ConsoleColor.Yellow);
            if (yesno)
            {
                var start = 0;
                var ynRecoverCompletely = Prompt.GetYesNo("All existing transactions will be dropped. Do you want to recover from the beginning?", false, ConsoleColor.Red);
                if (!ynRecoverCompletely)
                {
                    start = Prompt.GetInt("Recover from specific blockchain height or leave it blank to recover from your last transaction height:", 0, ConsoleColor.Magenta);
                }
                await Spinner.StartAsync("Recovering transactions ...", spinner =>
                {
                    var (recovered, message) = _commandReceiver.RecoverTransactions(activeSession, start, ynRecoverCompletely);
                    if (recovered is null)
                    {
                        spinner.Fail(message);
                    }

                    return Task.CompletedTask;
                }, Patterns.Pong);
            }

        }
    }
}