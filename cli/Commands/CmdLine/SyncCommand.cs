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

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("sync", "syncs wallet with chain")]
    public class SyncCommand : Command
    {
        public SyncCommand(IServiceProvider serviceProvider)
            : base(typeof(SyncCommand), serviceProvider, false)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                _commandReceiver.SyncWallet(activeSession);
                Debug.WriteLine("Syncing wallet with chain...");

            }
        }
    }
}