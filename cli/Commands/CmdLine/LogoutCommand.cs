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

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("logout", "Logs out and locks wallet.")]
    class LogoutCommand : Command
    {
        public LogoutCommand(IServiceProvider serviceProvider)
            : base(typeof(LogoutCommand), serviceProvider)
        {
        }
        public override void Execute(Session activeSession = null)
        {
        }
    }
}