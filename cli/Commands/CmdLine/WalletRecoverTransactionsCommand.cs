using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Kurukuru;

using BAMWallet.HD;
using BAMWallet.Helper;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("recover", "Recover wallet transactions")]
    public class WalletRecoverTransactionsCommand : Command
    {
        private readonly ICommandReceiver _walletService;

        public WalletRecoverTransactionsCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletRecoverTransactionsCommand), serviceProvider)
        {
            _walletService = serviceProvider.GetService<ICommandReceiver>();
        }

        public override void Execute()
        {
            this.Login();
            using var KeepLoginState = new RAIIGuard(Command.FreezeTimer, Command.UnfreezeTimer);
            Spinner.StartAsync("Recovering transactions ...", spinner =>
            {
                _walletService.RecoverTransactions(ActiveSession, 0);
                return Task.CompletedTask;
            }, Patterns.Pong);
        }
    }
}