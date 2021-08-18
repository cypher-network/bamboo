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
using BAMWallet.HD;
using BAMWallet.Helper;
using BAMWallet.Model;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("balance", "Get your wallet balance")]
    public class WalletBalanceCommand : Command
    {
        private readonly IWalletService _walletService;
        private Spinner _spinner;

        public WalletBalanceCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletBalanceCommand), serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override void Execute()
        {
            this.Login();
            using var KeepLoginState = new RAIIGuard(Command.FreezeTimer, Command.UnfreezeTimer);
            try
            {
                Spinner.StartAsync("Checking balance ...", spinner =>
                {
                    _spinner = spinner;
                    var session = ActiveSession;
                    var balanceResult = _walletService.History(session);
                    if(balanceResult.Item1 is null)
                    {
                        spinner.Fail(balanceResult.Item2);
                    }
                    else
                    {
                        var lastSheet = (balanceResult.Item1 as IOrderedEnumerable<BalanceSheet>).Last();
                        _console.ForegroundColor = ConsoleColor.Green;
                        _console.WriteLine($"\n Balance: {lastSheet.Balance}");
                        _console.ForegroundColor = ConsoleColor.White;
                        spinner.Succeed();
                    }
                    return Task.CompletedTask;
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
