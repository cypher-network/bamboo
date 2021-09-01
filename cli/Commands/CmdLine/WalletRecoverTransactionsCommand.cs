using System;
using System.Threading.Tasks;
using Kurukuru;
using Cli.Commands.Common;
using BAMWallet.HD;
namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("recover", "Recover wallet transactions")]
    public class WalletRecoverTransactionsCommand : Command
    {
        public WalletRecoverTransactionsCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletRecoverTransactionsCommand), serviceProvider, true)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                Spinner.StartAsync("Recovering transactions ...", spinner =>
                {
                    _commandReceiver.RecoverTransactions(activeSession, 0);
                    return Task.CompletedTask;
                }, Patterns.Pong);
            }
        }
    }
}