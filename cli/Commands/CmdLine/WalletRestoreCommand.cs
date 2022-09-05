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
using BAMWallet.Helper;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;
namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("restore", "Restore wallet from seed and passphrase")]
    class WalletRestoreCommand : Command
    {
        public WalletRestoreCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletRestoreCommand), serviceProvider)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            var walletName = Prompt.GetString("Specify new wallet name (e.g., MyWallet):", null, ConsoleColor.Red);
            using var seed = Prompt.GetPasswordAsSecureString("Seed:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase/pin:", ConsoleColor.Yellow);

            var id = await _commandReceiver.CreateWallet(seed, passphrase, walletName);
            var path = Util.WalletPath(id);

            _console.ForegroundColor = ConsoleColor.Yellow;
            _console.WriteLine("Your wallet has been generated!\n");
            _console.WriteLine("To start synchronizing with the network, login with your wallet name and passphrase or PIN\n");
            _console.ForegroundColor = ConsoleColor.White;
            _console.ForegroundColor = ConsoleColor.Green;
            _console.WriteLine($"Wallet Name: {id}");
            _console.WriteLine($"Wallet Path: {path}");
            _console.ForegroundColor = ConsoleColor.White;
        }
    }
}