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
using BAMWallet.Model;
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

                var networkSettings = BAMWallet.Helper.Util.LiteRepositoryAppSettingsFactory().Query<NetworkSettings>().First();
                if (networkSettings != null)
                {
                    // Quickest way to force users to create a new wallet if the network node doesn't match the pub address.
                    if (ActiveSession.KeySet.StealthAddress.StartsWith('v') && !networkSettings.Environment.Equals(Constant.Mainnet))
                    {
                        _console.WriteLine("Please create a separate wallet for mainnet. Or change the network environment back to mainnet.\nShutting down...");
                        Environment.Exit(0);
                        return;
                    }

                    if (ActiveSession.KeySet.StealthAddress.StartsWith('w') && !networkSettings.Environment.Equals(Constant.Testnet))
                    {
                        _console.WriteLine("Please create a separate wallet for testnet. Or change the network environment back to testnet.\nShutting down...");
                        Environment.Exit(0);
                        return;
                    }
                }

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