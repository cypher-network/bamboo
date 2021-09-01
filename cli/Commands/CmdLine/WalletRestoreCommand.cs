// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using BAMWallet.HD;
using BAMWallet.Helper;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;
namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("restore", "Restore wallet from seed")]
    class WalletRestoreCommand : Command
    {
        public WalletRestoreCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletRestoreCommand), serviceProvider)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            using var seed = Prompt.GetPasswordAsSecureString("Seed:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var id = _commandReceiver.CreateWallet(seed, passphrase);
            var path = Util.WalletPath(id);

            _console.WriteLine($"Wallet ID: {id}");
            _console.WriteLine($"Wallet Path: {path}");
        }
    }
}