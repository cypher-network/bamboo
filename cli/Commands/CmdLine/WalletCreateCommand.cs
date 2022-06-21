// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Security;
using System.Security.Cryptography;
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
            var passphrase = new SecureString();

            var walletName = Prompt.GetString("Specify wallet file name (e.g., MyWallet):", null, ConsoleColor.Yellow);
            var genPassOrPin = Prompt.GetYesNo("Generate a secure passphrase or pin?", false, ConsoleColor.Green);
            if (genPassOrPin)
            {
                var pinOrPass = Prompt.GetYesNo("Passphrase or pin?", true, ConsoleColor.Green);
                if (pinOrPass)
                {
                    passphrase = string.Join(" ", _commandReceiver.CreateSeed(NBitcoin.WordCount.Twelve)).ToSecureString();
                }

                if (!pinOrPass)
                {
                    passphrase = SecurePin();
                }
            }
            else
            {
                passphrase = Prompt.GetPasswordAsSecureString("Specify wallet passphrase/pin:", ConsoleColor.Yellow);
            }

            try
            {
                await Spinner.StartAsync("Creating wallet ...", async _ =>
                {
                    var defaultSeed = _commandReceiver.CreateSeed(NBitcoin.WordCount.TwentyFour);
                    var joinMmnemonic = string.Join(" ", defaultSeed);
                    var passTemp = passphrase.FromSecureString();
                    var id = await _commandReceiver.CreateWallet(joinMmnemonic.ToSecureString(), passphrase, walletName);
                    var path = Util.WalletPath(id);

                    _console.WriteLine();

                    _console.ForegroundColor = ConsoleColor.Yellow;
                    _console.WriteLine("Your wallet has been generated!");
                    _console.ForegroundColor = ConsoleColor.White;

                    _console.WriteLine();
                    
                    _console.ForegroundColor = ConsoleColor.Red;
                    _console.WriteLine("NOTE:\n" +
                                       "The following 24 seed word can be used to recover your wallet.\n" +
                                       "Please write down the 24 seed word and store it somewhere safe and secure.");
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
                    _console.WriteLine("Seed:");
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine($"{joinMmnemonic}");
                    _console.ForegroundColor = ConsoleColor.White;
                    _console.WriteLine("Passphrase/Pin:");
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine($"{passTemp}");
                    _console.ForegroundColor = ConsoleColor.White;

                    joinMmnemonic.ZeroString();
                    passTemp.ZeroString();
                    
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static SecureString SecurePin()
        {
            var buffer = new byte[sizeof(ulong)];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buffer);
            var num = BitConverter.ToUInt64(buffer, 0);
            var pin = num % 100000000;
            return pin.ToString("D8").ToSecureString();
        }
    }
}
