// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using BAMWallet.HD;
using Cli.Commands.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BAMWallet.Model;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("sync", "Syncs wallet with chain")]
    public class SyncCommand : Command
    {
        public SyncCommand(IServiceProvider serviceProvider)
            : base(typeof(SyncCommand), serviceProvider, false)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                await _commandReceiver.SyncWallet(activeSession);
            }
        }
    }
}