// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using BAMWallet.Extensions;
namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("logout", "Logs out and locks wallet.")]
    class Logout : Command
    {
        private readonly IConsole _console;

        public Logout(IServiceProvider serviceProvider) : base(typeof(Logout).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(Logout).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description))
        {
            _console = serviceProvider.GetService<IConsole>();
        }

        public override Task Execute()
        {
            Logout();
            return Task.CompletedTask;
        }
    }
}