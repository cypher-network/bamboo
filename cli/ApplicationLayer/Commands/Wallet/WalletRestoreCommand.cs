// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using McMaster.Extensions.CommandLineUtils;

using BAMWallet.HD;
using BAMWallet.Helper;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("restore", "Restore wallet from seed")]
    class WalletRestoreCommand : Command
    {
        private readonly IWalletService _walletService;

        public WalletRestoreCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletRestoreCommand), serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override void Execute()
        {
            using var seed = Prompt.GetPasswordAsSecureString("Seed:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var id = _walletService.CreateWallet(seed, passphrase);
            var path = Util.WalletPath(id);

            _console.WriteLine($"Wallet ID: {id}");
            _console.WriteLine($"Wallet Path: {path}");
        }
    }
}