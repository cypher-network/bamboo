// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Timers;
using BAMWallet.HD;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System;
using BAMWallet.Extensions;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("sync", "syncs wallet with chain")]
    public class SyncCommand : Command
    {
        private readonly double SYNC_INTERVAL = 1000 * 60 * 5;
        private IWalletService _walletService;
        private readonly Timer _syncTimer;
        public bool IsSynchronizing { get; private set; }

        public SyncCommand(IWalletService walletService, IServiceProvider serviceProvider) : base(typeof(SyncCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(SyncCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description), serviceProvider.GetService<IConsole>())
        {
            _walletService = walletService;
            _syncTimer = new Timer(SYNC_INTERVAL);
            _syncTimer.Elapsed += OnSyncInternal;
            _syncTimer.AutoReset = true;
            _syncTimer.Start();
        }

        public override async Task Execute()
        {
            if (Command.ActiveSession != null)
            {
                await _walletService.SyncWallet(Command.ActiveSession);
            }
        }

        private void OnSyncInternal(object source, ElapsedEventArgs e)
        {
            if (!_walletService.IsCommandExecutionInProgress)
            {
                IsSynchronizing = true;
                Execute().Wait();
                IsSynchronizing = false;
            }
        }
    }
}