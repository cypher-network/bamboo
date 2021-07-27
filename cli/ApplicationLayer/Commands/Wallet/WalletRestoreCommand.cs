// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using BAMWallet.HD;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.Helper;
namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("restore", "Restore wallet from mnemonic")]
    class WalletRestoreCommand : Command
    {
        private readonly IConsole _console;
        private readonly IWalletService _walletService;

        public WalletRestoreCommand(IServiceProvider serviceProvider) : base(typeof(WalletRestoreCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(WalletRestoreCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description))
        {
            _console = serviceProvider.GetService<IConsole>();
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override Task Execute()
        {
            using var mnemonic = Prompt.GetPasswordAsSecureString("Mnemonic:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var id = _walletService.CreateWallet(mnemonic, passphrase);
            var path = Util.WalletPath(id);

            _console.WriteLine($"Wallet ID: {id}");
            _console.WriteLine($"Wallet Path: {path}");
            return Task.CompletedTask;
        }
    }
}