// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Timers;

namespace BAMWallet.HD
{
    public class SyncManager
    {
        private IWalletService _walletService;
        private readonly Timer _syncTimer;
        public bool IsSynchronizing { get; private set; }

        public SyncManager(IWalletService walletService)
        {
            _walletService = walletService;
        }
    }
}