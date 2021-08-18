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
using ConsoleTables;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("history", "Show my transactions")]
    public class WalletTxHistoryCommand : Command
    {
        private readonly IWalletService _walletService;

        public WalletTxHistoryCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletTxHistoryCommand), serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override void Execute()
        {
            this.Login();
            using var KeepLoginState = new RAIIGuard(Command.FreezeTimer, Command.UnfreezeTimer);
            try
            {
                var session = ActiveSession;

                Spinner.StartAsync("Looking up history ...", spinner =>
                {
                    var balanceResult = _walletService.History(session);
                    if (balanceResult.Item1 is null)
                    {
                        _console.ForegroundColor = ConsoleColor.Red;
                        spinner.Fail($"balanceResult.Item2");
                        _console.ForegroundColor = ConsoleColor.White;
                        return Task.CompletedTask;
                    }
                    else
                    {
                        var balance = (balanceResult.Item1 as IOrderedEnumerable<BalanceSheet>);

                        if (!balance.Any())
                        {
                            NoTxn();
                            return Task.CompletedTask;
                        }

                        var table = new ConsoleTable(
                            "DateTime",
                            "Payment Id",
                            "Memo",
                            "Money In",
                            "Money Out",
                            "Reward",
                            "Balance",
                            "Verified",
                            "Locked");

                        foreach (var sheet in balance)
                        {
                            table.AddRow(
                                sheet.Date,
                                sheet.TxId,
                                sheet.Memo,
                                sheet.MoneyIn,
                                sheet.MoneyOut,
                                sheet.Reward,
                                sheet.Balance,
                                sheet.IsVerified.ToString(),
                                sheet.IsLocked.ToString());
                        }

                        table.Configure(o => o.NumberAlignment = Alignment.Right);

                        _console.WriteLine($"\n{table}");

                        return Task.CompletedTask;
                    }
                });
            }
            catch (Exception ex)
            {
                NoTxn(ex);
            }
        }

        private void NoTxn(Exception ex = null)
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine($"\nWallet has no transactions.\n");
            if (ex != null)
                _console.WriteLine(ex.Message);
            _console.ForegroundColor = ConsoleColor.White;
        }
    }
}
