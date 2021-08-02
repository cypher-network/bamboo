// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Timers;
using BAMWallet.HD;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using BAMWallet.Extensions;
using Kurukuru;
using CLi.ApplicationLayer.Events;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("sync", "syncs wallet with chain")]
    public class SyncCommand : Command
    {
        private static readonly double SYNC_INTERVAL = 1000 * 60 * 1;
        private IWalletService _walletService;
        private static readonly Timer _syncTimer = new Timer(SYNC_INTERVAL);
        public static bool IsSynchronizing { get; private set; }
        public static EventHandler<SyncStateChanged> OnSyncStateChanged;

        public SyncCommand(IWalletService walletService, IServiceProvider serviceProvider) : base(typeof(SyncCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(SyncCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description), serviceProvider.GetService<IConsole>())
        {
            _walletService = walletService;
            _syncTimer.Elapsed += OnSyncInternal;
            _syncTimer.AutoReset = true;
            _syncTimer.Start();
            OnSyncStateChanged?.Invoke(this, new SyncStateChanged(SyncStateChanged.SyncState.Idle));
        }

        public override async Task Execute()
        {
            if (Command.ActiveSession != null)
            {
                _console.WriteLine("Syncing wallet with chain ...");
                _console.ForegroundColor = ConsoleColor.Cyan;
                _console.Write("bamboo$ ");
                _console.ForegroundColor = ConsoleColor.White;
                await _walletService.SyncWallet(Command.ActiveSession);
            }
        }

        private void OnSyncInternal(object source, ElapsedEventArgs e)
        {
            if (!_walletService.IsCommandExecutionInProgress)
            {
                OnSyncStateChanged?.Invoke(this, new SyncStateChanged(SyncStateChanged.SyncState.SyncInProgress));
                IsSynchronizing = true;
                Execute().Wait();
                IsSynchronizing = false;
                OnSyncStateChanged?.Invoke(this, new SyncStateChanged(SyncStateChanged.SyncState.Idle));
            }
        }
    }
}