// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using BAMWallet.HD;
namespace Cli.Commands.Common
{
    [CommandDescriptor("exit", "Exit the wallet")]
    public class ExitCommand : Command
    {
        public ExitCommand(IServiceProvider serviceProvider)
            : base(typeof(ExitCommand), serviceProvider)
        {
        }

        public override Task Execute(Session activeSession = null)
        {
            return Task.CompletedTask;
        }
    }
}
