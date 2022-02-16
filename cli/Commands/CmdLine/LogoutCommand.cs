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
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("logout", "Session ends and locks the wallet")]
    class LogoutCommand : Command
    {
        private bool _automaticLogout;
        public LogoutCommand(IServiceProvider serviceProvider, bool automaticLogout = false)
            : base(typeof(LogoutCommand), serviceProvider)
        {
            _automaticLogout = automaticLogout;
        }
        public override Task Execute(Session activeSession = null)
        {
            if (_automaticLogout)
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("You have been logged out of the wallet due to inactivity. Please login again to use the wallet.");
                _console.ForegroundColor = ConsoleColor.Cyan;
                _console.Write("bamboo$ ");
                _console.ResetColor();
            }

            return Task.CompletedTask;
        }

        public bool AutomaticLogout
        {
            get
            {
                return _automaticLogout;
            }
        }
    }
}