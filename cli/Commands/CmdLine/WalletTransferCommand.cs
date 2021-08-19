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
using Microsoft.Extensions.Logging;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("spend", "Spend some coins")]
    public class WalletTransferCommand : Command
    {
        private readonly ILogger _logger;
        private Spinner _spinner;

        public WalletTransferCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletTransferCommand), serviceProvider, true)
        {
            _logger = serviceProvider.GetService<ILogger<WalletTransferCommand>>();
        }

        public override void Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                var address = Prompt.GetString("Address:", null, ConsoleColor.Red);
                var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
                var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);
                var delay = Prompt.GetInt("Higher value ≈ faster transaction:", 5, ConsoleColor.Magenta);

                if (decimal.TryParse(amount, out var t))
                {
                    Spinner.StartAsync("Processing payment ...", spinner =>
                    {
                        _spinner = spinner;

                        try
                        {
                            var session = activeSession;

                            session.SessionType = SessionType.Coin;
                            var transaction = new WalletTransaction
                            {
                                Memo = memo,
                                Payment = t.ConvertToUInt64(),
                                RecipientAddress = address,
                                WalletType = WalletType.Send,
                                Delay = delay,
                                IsVerified = false,
                                SenderAddress = session.KeySet.StealthAddress
                            };

                            var createTransactionResult = _walletService.CreateTransaction(session, ref transaction);
                            if (createTransactionResult.Item1 is null)
                            {
                                spinner.Fail(createTransactionResult.Item2);
                            }
                            var sendResult = _walletService.Send(session, ref transaction);
                            if (sendResult.Item1 is null)
                            {
                                spinner.Fail(sendResult.Item2);
                            }
                            else
                            {
                                var balanceResult = _walletService.History(session);
                                if (balanceResult.Item1 is null)
                                {
                                    spinner.Fail(balanceResult.Item2);
                                }
                                else
                                {
                                    var message = $"Available Balance: {(balanceResult.Item1 as IOrderedEnumerable<BalanceSheet>).Last().Balance}";
                                    message += $"\nPaymentID: {transaction.Transaction.TxnId.ByteToHex()}";
                                    activeSession.SessionId = Guid.NewGuid();
                                    spinner.Succeed(message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            activeSession.SessionId = Guid.NewGuid();
                            _logger.LogError(ex.StackTrace);
                            throw;
                        }
                        return Task.CompletedTask;
                    }, Patterns.Toggle3);
                }
            }
        }
    }
}
