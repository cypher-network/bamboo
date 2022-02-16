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

        public override void Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                Spinner.StartAsync("Recovering transactions ...", spinner =>
                {
                    var (recovered, message) = _commandReceiver.RecoverTransactions(activeSession, 0);
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