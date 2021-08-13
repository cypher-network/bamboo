﻿// Bamboo (c) by Tangram
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

using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using BAMWallet.Model;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("spend", "Spend some coins")]
    public class WalletTransferCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly ILogger _logger;

        private Spinner _spinner;

        public WalletTransferCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletTransferCommand), serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _logger = serviceProvider.GetService<ILogger<WalletTransferCommand>>();
        }

        public override void Execute()
        {
            this.Login();
            using var KeepLoginState = new RAIIGuard(Command.FreezeTimer, Command.UnfreezeTimer);
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
                        var session = ActiveSession;

                        session.SessionType = SessionType.Coin;
                        session.WalletTransaction = new WalletTransaction
                        {
                            Memo = memo,
                            Payment = t.ConvertToUInt64(),
                            RecipientAddress = address,
                            WalletType = WalletType.Send,
                            Delay = delay,
                            IsVerified = false
                        };

                        _walletService.CreateTransaction(session);

                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                        }

                        if (!_walletService.Send(session).Item1)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
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
                    return Task.CompletedTask;
                }, Patterns.Toggle3);
            }
        }
    }
}
