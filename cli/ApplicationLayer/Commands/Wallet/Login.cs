// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;

using McMaster.Extensions.CommandLineUtils;

using BAMWallet.HD;

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
            Session session = new Session(identifier, passphrase); //will throw if wallet doesn't exist
            Command.ActiveSession = session;
            Login();
        }
    }
}