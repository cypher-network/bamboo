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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using McMaster.Extensions.CommandLineUtils;
using Kurukuru;
using BAMWallet.HD;
using BAMWallet.Model;
using BAMWallet.Extensions;
using BAMWallet.Helper;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("spend", "Spend some coins")]
    public class WalletTransferCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly ILogger _logger;

        private Spinner _spinner;

        public WalletTransferCommand(IServiceProvider serviceProvider) : base(typeof(WalletTransferCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(WalletTransferCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description), serviceProvider.GetService<IConsole>())
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _logger = serviceProvider.GetService<ILogger<WalletTransferCommand>>();
        }

        public override async Task Execute()
        {
            this.Login();
            using var KeepLoginState = new RAIIGuard(Command.FreezeTimer, Command.UnfreezeTimer);
            var address = Prompt.GetString("Address:", null, ConsoleColor.Red);
            var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
            var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);
            var delay = Prompt.GetInt("Higher value ≈ faster transaction:", 5, ConsoleColor.Magenta);

            if (decimal.TryParse(amount, out var t))
            {
                await Spinner.StartAsync("Processing payment ...", async spinner =>
                {
                    _spinner = spinner;

                    try
                    {
                        var session = ActiveSession;

                        session.SessionType = SessionType.Coin;
                        session.WalletTransaction = new WalletTransaction
                        {
                            Memo = memo,
                            Payment = t.ConvertToUInt64(),
                            RecipientAddress = address,
                            WalletType = WalletType.Send,
                            Delay = delay
                        };

                        _walletService.CreateTransaction(session);

                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                            return;
                        }

                        await _walletService.Send(session);

                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                            return;
                        }

                        var balance = _walletService.History(session);
                        var message = $"Available Balance: {balance.Result.Last().Balance}";

                        var walletTx = _walletService.GetTransaction(session);
                        if (walletTx != null)
                        {
                            message += $"\nPaymentID: {walletTx?.TxnId.ByteToHex()}";
                        }
                        ActiveSession.SessionId = Guid.NewGuid();
                        spinner.Succeed(message);
                    }
                    catch (Exception ex)
                    {
                        ActiveSession.SessionId = Guid.NewGuid();
                        _logger.LogError(ex.StackTrace);
                        throw;
                    }
                }, Patterns.Toggle3);
            }
        }
    }
}
