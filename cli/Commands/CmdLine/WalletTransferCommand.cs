// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
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
    [CommandDescriptor("spend", "Spend some crypto")]
    public class WalletTransferCommand : Command
    {
        private readonly ILogger _logger;

        public WalletTransferCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletTransferCommand), serviceProvider, true)
        {
            _logger = serviceProvider.GetService<ILogger<WalletTransferCommand>>();
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                var address = Prompt.GetString("Address/Name:", null, ConsoleColor.Red);
                var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
                var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);
                var delay = Prompt.GetInt("Priority:", 5, ConsoleColor.Magenta);
                if (decimal.TryParse(amount, out var t))
                {
                    if (!_commandReceiver.IsBase58(address))
                    {
                        var addressBook = new AddressBook { Name = address };
                        var result = _commandReceiver.FindAddressBook(activeSession, ref addressBook);
                        if (result.Item1 != null)
                        {
                            address = (result.Item1 as AddressBook)?.RecipientAddress;
                            var yesno = Prompt.GetYesNo($"Address for {addressBook.Name}?\n(** {address?.ToUpper()} **)",
                                false, ConsoleColor.Red);
                            if (!yesno)
                            {
                                _console.ForegroundColor = ConsoleColor.Green;
                                _console.WriteLine("Processing transaction cancelled!");
                                _console.ForegroundColor = ConsoleColor.White;
                                return;
                            }
                        }
                        else
                        {
                            _console.ForegroundColor = ConsoleColor.Red;
                            _console.WriteLine(result.Item2);
                            _console.ForegroundColor = ConsoleColor.White;
                            return;
                        }
                    }

                    await Spinner.StartAsync("Processing transaction ...", spinner =>
                    {
                        try
                        {
                            activeSession.SessionType = SessionType.Coin;
                            var transaction = new WalletTransaction
                            {
                                Memo = memo,
                                Payment = t.ConvertToUInt64(),
                                RecipientAddress = address,
                                WalletType = WalletType.Send,
                                Delay = delay,
                                IsVerified = false,
                                SenderAddress = activeSession.KeySet.StealthAddress
                            };
                            var createTransactionResult =
                                _commandReceiver.CreateTransaction(activeSession, ref transaction);
                            if (createTransactionResult.Item1 is null)
                            {
                                spinner.Fail(createTransactionResult.Item2);
                            }
                            else
                            {
                                var sendResult = _commandReceiver.SendTransaction(activeSession, ref transaction);
                                if (sendResult.Item1 is not true)
                                {
                                    spinner.Fail(sendResult.Item2);
                                }
                                else
                                {
                                    var balanceResult = _commandReceiver.History(activeSession);
                                    if (balanceResult.Item1 is null)
                                    {
                                        spinner.Fail(balanceResult.Item2);
                                    }
                                    else
                                    {
                                        var message =
                                            $"Available Balance: [{(balanceResult.Item1 as IList<BalanceSheet>).Last().Balance}] " +
                                            $"TxID: ({transaction.Transaction.TxnId.ByteToHex()}) " +
                                            $"Tx size: [{transaction.Transaction.GetSize() / 1024}kB]";
                                        activeSession.SessionId = Guid.NewGuid();
                                        spinner.Succeed(message);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            activeSession.SessionId = Guid.NewGuid();
                            _logger.LogError($"Message: {ex.Message}\nStack Trace:{ex.StackTrace}");
                            throw;
                        }

                        return Task.CompletedTask;
                    }, Patterns.Toggle3);
                }
            }
        }
    }
}
