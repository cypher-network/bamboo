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
using NBitcoin;

using BAMWallet.HD;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("seed", "Creates a new seed and passphrase")]
    class WalletCreateMnemonicCommand : Command
    {
        private readonly IWalletService _walletService;

        public WalletCreateMnemonicCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletCreateMnemonicCommand), serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override void Execute()
        {
            _console.ForegroundColor = ConsoleColor.Magenta;

            _console.WriteLine("\nSeed phrase\n");

            Options(out Language lang, out WordCount wCount, 3);

            var seed = _walletService.CreateSeed(lang, wCount);

            _console.ForegroundColor = ConsoleColor.Magenta;

            _console.WriteLine("");
            _console.WriteLine("Passphrase");

            Options(out lang, out wCount, 1);

            var passphrase = _walletService.CreateSeed(lang, wCount);

            _console.WriteLine("Seed phrase: " + string.Join(" ", seed));
            _console.WriteLine("Passphrase:  " + string.Join(" ", passphrase));

            _console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="wCount"></param>
        private void Options(out Language lang, out WordCount wCount, int defaultAnswer)
        {
            _console.WriteLine("\nLanguage:\n");
            _console.WriteLine("English              [0]");
            _console.WriteLine("Japanese             [1]");
            _console.WriteLine("Spanish              [2]");
            _console.WriteLine("ChineseSimplified    [3]");
            _console.WriteLine("ChineseTraditional   [4]");
            _console.WriteLine("French               [5]");
            _console.WriteLine("PortugueseBrazil     [6]");
            _console.WriteLine("Czech                [7]\n");

            var language = Prompt.GetInt("Select language:", 0, ConsoleColor.Yellow);

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

            lang = (Language)language;
            wCount = (WordCount)wordCount;
        }
    }
}
