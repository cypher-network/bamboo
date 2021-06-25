using System;
using System.Threading.Tasks;
using BAMWallet.HD;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "recover" }, "Recover wallet transactions")]
    public class WalletRecoverTransactionsCommand : Command
    {
        private readonly IConsole _console;
        private readonly IWalletService _walletService;

        private Spinner _spinner;

        public WalletRecoverTransactionsCommand(IServiceProvider serviceProvider)
        {
            _console = serviceProvider.GetService<IConsole>();
            _walletService = serviceProvider.GetService<IWalletService>();
        }

        public override async Task Execute()
        {
            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);
            await Spinner.StartAsync("Recovering transactions ...", async spinner =>
            {
                _spinner = spinner;
                var session = _walletService.SessionAddOrUpdate(new Session(identifier, passphrase));

                await _walletService.RecoverTransactions(session.SessionId, 0);

                return Task.CompletedTask;
            }, Patterns.Pong);
        }
    }
}