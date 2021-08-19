// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using BAMWallet.HD;
using Cli.Commands.Common;
namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("sync", "syncs wallet with chain")]
    public class SyncCommand : Command
    {
        public SyncCommand(IServiceProvider serviceProvider)
            : base(typeof(SyncCommand), serviceProvider, false)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                _console.WriteLine("Syncing wallet with chain ...");
                _console.ForegroundColor = ConsoleColor.Cyan;
                _console.Write("bamboo$ ");
                _console.ForegroundColor = ConsoleColor.White;
                _walletService.SyncWallet(activeSession);
            }
        }
    }
}