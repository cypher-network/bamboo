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
using BAMWallet.Extensions;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("create", "Create new wallet")]
    class WalletCreateCommand : Command
    {
        private readonly IWalletService _walletService;

        public WalletCreateCommand(IServiceProvider serviceProvider) : base(typeof(WalletCreateCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(WalletCreateCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description), serviceProvider.GetService<IConsole>())
        {
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override async Task Execute()
        {
            try
            {
                var mnemonicDefault = await _walletService.CreateSeed(NBitcoin.Language.English, NBitcoin.WordCount.TwentyFour);
                var passphraseDefault = await _walletService.CreateSeed(NBitcoin.Language.English, NBitcoin.WordCount.Twelve);
                var joinMmnemonic = string.Join(" ", mnemonicDefault);
                var joinPassphrase = string.Join(" ", passphraseDefault);
                var id = _walletService.CreateWallet(joinMmnemonic.ToSecureString(), joinPassphrase.ToSecureString());
                var path = Util.WalletPath(id);

                _console.WriteLine();

                _console.WriteLine("Wallet Path:");
                _console.ForegroundColor = ConsoleColor.Green;
                _console.WriteLine($"{path}");
                _console.ForegroundColor = ConsoleColor.White;

                _console.WriteLine();

                _console.WriteLine("Wallet ID:");
                _console.ForegroundColor = ConsoleColor.Green;
                _console.WriteLine($"{id}");
                _console.ForegroundColor = ConsoleColor.White;

                _console.WriteLine();

                _console.WriteLine("Seed phrase:");
                _console.ForegroundColor = ConsoleColor.Green;
                _console.WriteLine($"{joinMmnemonic}");
                _console.ForegroundColor = ConsoleColor.White;

                _console.WriteLine();

                _console.WriteLine("Passphrase:");
                _console.ForegroundColor = ConsoleColor.Green;
                _console.WriteLine($"{joinPassphrase}");
                _console.ForegroundColor = ConsoleColor.White;


                _console.WriteLine();

                joinMmnemonic.ZeroString();
                joinPassphrase.ZeroString();
            }
            catch (Exception ex)
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"{ex.Message}");
                _console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
