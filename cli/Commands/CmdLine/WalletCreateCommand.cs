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
using BAMWallet.Helper;
using Cli.Commands.Common;
using Kurukuru;
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

        public override async Task Execute(Session activeSession = null)
        {
            var walletName = Prompt.GetString("Specify wallet file name (e.g., MyWallet):", null, ConsoleColor.Yellow);

            try
            {
                await Spinner.StartAsync("Creating wallet ...", async spinner =>
                {
                    var seedDefault = _commandReceiver.CreateSeed(NBitcoin.WordCount.TwentyFour);
                    var passPhraseDefault = _commandReceiver.CreateSeed(NBitcoin.WordCount.Twelve);
                    var joinMmnemonic = string.Join(" ", seedDefault);
                    var joinPassphrase = string.Join(" ", passPhraseDefault);
                    var id = await _commandReceiver.CreateWallet(joinMmnemonic.ToSecureString(),
                        joinPassphrase.ToSecureString(), walletName);
                    var path = Util.WalletPath(id);

                    _console.WriteLine();

                    _console.ForegroundColor = ConsoleColor.Yellow;
                    _console.WriteLine("Your wallet has been generated!");
                    _console.ForegroundColor = ConsoleColor.White;

                    _console.WriteLine();

                    _console.WriteLine("Your wallet can be found here:");
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine($"{path}");
                    _console.ForegroundColor = ConsoleColor.White;

                    _console.WriteLine();

                    _console.WriteLine("Wallet Name:");
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine($"{id}");
                    _console.ForegroundColor = ConsoleColor.White;

                    _console.WriteLine();

                    _console.ForegroundColor = ConsoleColor.Red;
                    _console.WriteLine("PLEASE NOTE:\n" +
                                       "The following Seed and Passphrase words can be used to recover access to your wallet.\n" +
                                       "Please write them down and store them somewhere safe and secure.");
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

                    joinMmnemonic.ZeroString();
                    joinPassphrase.ZeroString();

                    _console.WriteLine();

                    return Task.CompletedTask;
                }, Patterns.Hearts);
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
