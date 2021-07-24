// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BAMWallet.Extensions;

namespace CLi.ApplicationLayer.Commands.Vault
{
    [CommandDescriptor("exit", "Exit the wallet")]
    public class ExitCommand : Command
    {
        ICommandService commandService;

        public ExitCommand(ICommandService service): base(typeof(ExitCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(ExitCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description))
        {
            commandService = service;
        }

        public override async Task Execute()
        {
            await commandService.Exit();
        }
    }
}
