// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using Microsoft.Extensions.DependencyInjection;
using BAMWallet.Extensions;
using BAMWallet.HD;
using McMaster.Extensions.CommandLineUtils;
namespace Cli.Commands.Common
{
    public abstract class Command
    {
        private bool _refreshLogin;
        protected readonly ICommandReceiver _walletService;
        protected readonly ICommandService _receiver;
        protected readonly IConsole _console;

        protected Command(Type commandType, IServiceProvider serviceProvider, bool refreshLogin = false)
        {
            Name = commandType.GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name);
            Description = commandType.GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description);
            _walletService = serviceProvider.GetService<ICommandReceiver>();
            _receiver = serviceProvider.GetService<ICommandService>();
            _console = serviceProvider.GetService<IConsole>();
            _refreshLogin = refreshLogin;
        }

        public abstract void Execute(Session activeSession = null);

        public string Name { get; set; }
        public string Description { get; set; }

        public bool RefreshLogin
        {
            get
            {
                return _refreshLogin;
            }
        }
    }
}