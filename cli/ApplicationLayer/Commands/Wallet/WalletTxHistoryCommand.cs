// Bamboo (c) by Tangram 
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using McMaster.Extensions.CommandLineUtils;

using ConsoleTables;

using BAMWallet.HD;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "history" }, "Show me my transactions")]
    public class WalletTxHistoryCommand : Command
    {
        private readonly IConsole _console;
        private readonly IWalletService _walletService;

        public WalletTxHistoryCommand(IServiceProvider serviceProvider)
        {
            _console = serviceProvider.GetService<IConsole>();
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override Task Execute()
        {
            using (var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow))
            using (var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow))
            {
                try
                {
                    var session = _walletService.SessionAddOrUpdate(new Session(identifier, passphrase));
                    var final = _walletService.History(session.SessionId).ToList();

                    if (final?.Any() == true)
                    {
                        var table = ConsoleTable.From(final).ToString();
                        _console.WriteLine($"\n{table}");

                        return Task.CompletedTask;
                    }

                    NoTxn();
                }
                catch (Exception)
                {
                    NoTxn();
                }
            }

            return Task.CompletedTask;
        }

        private void NoTxn()
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine($"\nWallet has no transactions.\n");
            _console.ForegroundColor = ConsoleColor.White;
        }
    }
}
