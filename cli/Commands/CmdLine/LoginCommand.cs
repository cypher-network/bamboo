// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using Cli.Commands.Common;
using Kurukuru;
using LiteDB;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("login", "Provide your wallet name and passphrase")]
    class LoginCommand : Command
    {
        private Session _session = null;
        public LoginCommand(IServiceProvider serviceProvider)
            : base(typeof(LoginCommand), serviceProvider, true)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            //check if wallet exists, if it does, save session, login and inform command service
            var identifier = Prompt.GetString("Wallet Name:", null, ConsoleColor.Yellow);
            var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);
            if (Session.AreCredentialsValid(identifier.ToSecureString(), passphrase))
            {
                ActiveSession = new Session(identifier.ToSecureString(), passphrase); //will throw if wallet doesn't exist
                await Spinner.StartAsync("Syncing wallet ...", async spinner =>
                {
                    await _commandReceiver.SyncWallet(ActiveSession);
                });
            }
            else
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("Access denied. Cannot find a wallet with the given identifier and passphrase.");
                _console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public Session ActiveSession
        {
            get
            {
                return _session;
            }
            private set
            {
                _session = value;
            }
        }
    }
}