// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Timers;
using System.Threading.Tasks;

using BAMWallet.HD;
using CLi.ApplicationLayer.Events;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("sync", "syncs wallet with chain")]
    public class SyncCommand : Command
    {
        private IWalletService _walletService;
        private readonly Timer _syncTimer;
        public static bool IsSynchronizing { get; private set; }
        public static EventHandler<SyncStateChanged> OnSyncStateChanged;

        public SyncCommand(IWalletService walletService, IServiceProvider serviceProvider)
            : base(typeof(SyncCommand), serviceProvider)
        {
            _syncTimer = new Timer(TimeSpan.FromMinutes(_timingSettings.SyncIntervalMins).TotalMilliseconds);
            _walletService = walletService;
            _syncTimer.Elapsed += OnSyncInternal;
            _syncTimer.AutoReset = true;
            _syncTimer.Start();
            OnSyncStateChanged?.Invoke(this, new SyncStateChanged(SyncStateChanged.SyncState.Idle));
        }

        public override void Execute()
        {
            if (Command.ActiveSession != null)
            {
                // _console.WriteLine("Syncing wallet with chain ...");
                // _console.ForegroundColor = ConsoleColor.Cyan;
                // _console.Write("bamboo$ ");
                // _console.ForegroundColor = ConsoleColor.White;
                _walletService.SyncWallet(Command.ActiveSession);
            }
        }

        private void OnSyncInternal(object source, ElapsedEventArgs e)
        {
            if (!_walletService.IsCommandExecutionInProgress)
            {
                OnSyncStateChanged?.Invoke(this, new SyncStateChanged(SyncStateChanged.SyncState.SyncInProgress));
                IsSynchronizing = true;
                Execute();
                IsSynchronizing = false;
                OnSyncStateChanged?.Invoke(this, new SyncStateChanged(SyncStateChanged.SyncState.Idle));
            }
        }
    }
}