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
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;
using NBitcoin;
namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("seed", "Create new seed and passphrase")]
    class WalletCreateMnemonicCommand : Command
    {
        public WalletCreateMnemonicCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletCreateMnemonicCommand), serviceProvider, true)
        {
        }

        public override Task Execute(Session activeSession = null)
        {
            _console.ForegroundColor = ConsoleColor.Magenta;
            _console.WriteLine("\nSeed phrase\n");

            Options(out WordCount wCount, 3);

            var seed = _commandReceiver.CreateSeed(wCount);
            _console.ForegroundColor = ConsoleColor.Magenta;
            _console.WriteLine("");
            _console.WriteLine("Passphrase");

            Options(out wCount, 1);
            var passphrase = _commandReceiver.CreateSeed(wCount);

            _console.WriteLine("Seed phrase: " + string.Join(" ", seed));
            _console.WriteLine("Passphrase:  " + string.Join(" ", passphrase));

            _console.ForegroundColor = ConsoleColor.White;

            return Task.CompletedTask;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="wCount"></param>
        /// <param name="defaultAnswer"></param>
        private void Options(out WordCount wCount, int defaultAnswer)
        {
            _console.ForegroundColor = ConsoleColor.Magenta;

            _console.WriteLine("\nWord Count:\n");
            _console.WriteLine("12    [1]");
            _console.WriteLine("18    [2]");
            _console.WriteLine("24    [3]\n");

            var wordCount = Prompt.GetInt("Select word count:", defaultAnswer, ConsoleColor.Yellow);

            _console.WriteLine("");

            switch (wordCount)
            {
                case 1:
                    wordCount = 12;
                    break;
                case 2:
                    wordCount = 18;
                    break;
                case 3:
                    wordCount = 24;
                    break;
                default:
                    break;
            }
            wCount = (WordCount)wordCount;
        }
    }
}
