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

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "spend" }, "Spend some coins")]
    public class WalletTransferCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly ILogger _logger;

        private Spinner _spinner;

        public WalletTransferCommand(IServiceProvider serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _logger = serviceProvider.GetService<ILogger<WalletTransferCommand>>();
        }

        public override async Task Execute()
        {
            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

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
                        var session = _walletService.SessionAddOrUpdate(new Session(identifier, passphrase)
                        {
                            SessionType = SessionType.Coin,
                            WalletTransaction = new WalletTransaction
                            {
                                Memo = memo,
                                Payment = t.ConvertToUInt64(),
                                RecipientAddress = address,
                                WalletType = WalletType.Send,
                                Delay = delay
                            }
                        });

                        _walletService.CreateTransaction(session.SessionId);

                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                            return;
                        }

                        await _walletService.Send(session.SessionId);

                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                            return;
                        }

                        var balance = _walletService.History(session.SessionId);
                        var message = $"Available Balance: {balance.Result.Last().Balance}";

                        var walletTx = _walletService.GetTransaction(session.SessionId);
                        if (walletTx != null)
                        {
                            message += $"\nPaymentID: {walletTx?.TxnId.ByteToHex()}";
                        }

                        spinner.Succeed(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                        throw;
                    }
                }, Patterns.Toggle3);
            }
        }
    }
}
