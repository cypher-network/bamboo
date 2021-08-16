// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Security;
using McMaster.Extensions.CommandLineUtils;
using BAMWallet.HD;
using LiteDB;
using BAMWallet.Helper;
using BAMWallet.Extensions;
using BAMWallet.Model;
namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("login", "Unlocks wallet and enables wallet commands.")]
    class Login : Command
    {
        public Login(IServiceProvider serviceProvider)
            : base(typeof(Login), serviceProvider)
        {
        }

        public override void Execute()
        {
            //check if wallet exists, if it does, save session, login and inform command service
            var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);
            if (Session.IsIdentifierValid(identifier) && ValidatePassPhrase(identifier, passphrase))
            {
                Session session = new Session(identifier, passphrase); //will throw if wallet doesn't exist
                Command.ActiveSession = session;
                Login();
            }
            else
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("Access denied. Cannot find a wallet with the given identifier and passphrase.");
                _console.ForegroundColor = ConsoleColor.Cyan;
                _console.Write("bamboo$ ");
                _console.ForegroundColor = ConsoleColor.White;
            }
        }

        private bool ValidatePassPhrase(SecureString id, SecureString pass)
        {
            var connectionString = new ConnectionString
            {
                Filename = Util.WalletPath(id.ToUnSecureString()),
                Password = pass.ToUnSecureString(),
                Connection = ConnectionType.Shared
            };
            using (var db = new LiteDatabase(connectionString))
            {
                var collection = db.GetCollection<KeySet>();
                try
                {
                    if (collection.Count() == 1)
                    {
                        return true;
                    }
                }
                catch (LiteException)
                {
                    return false;
                }
            }
            return false;
        }
    }
}