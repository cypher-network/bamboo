// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("create", "Create new wallet")]
    class WalletCreateCommand : Command
    {
        public WalletCreateCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletCreateCommand), serviceProvider, true)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            try
            {
                var seedDefault = _walletService.CreateSeed(NBitcoin.Language.English, NBitcoin.WordCount.TwentyFour);
                var passPhraseDefault = _walletService.CreateSeed(NBitcoin.Language.English, NBitcoin.WordCount.Twelve);
                var joinMmnemonic = string.Join(" ", seedDefault);
                var joinPassphrase = string.Join(" ", passPhraseDefault);
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

                _console.WriteLine("Seed:");
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
