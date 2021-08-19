// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using Cli.Commands.Common;
using BAMWallet.HD;

using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("version", "Running version")]
    public class WalletVersionCommand : Command
    {
        public WalletVersionCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletVersionCommand), serviceProvider)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            _console.WriteLine($"{BAMWallet.Helper.Util.GetAssemblyVersion()}");
        }
    }
}