// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using CLi.ApplicationLayer.Events;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using BAMWallet.Extensions;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor( "login" , "Unlocks wallet and enables wallet commands.")]
    class Login : Command
    {
        private readonly IConsole _console;
        public event EventHandler<LogInStateChanged> LoginStateChanged;

        public Login(IServiceProvider serviceProvider) : base(typeof(Login).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(Login).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description))
        {
            _console = serviceProvider.GetService<IConsole>();
        }

        public override Task Execute()
        {
            //check if wallet exists, if it does, save session, login and inform command service
            return Task.CompletedTask;
        }
    }
}