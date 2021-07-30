using System;
using Dawn;

namespace BAMWallet.Helper
{
    public sealed class RAIIGuard : IDisposable
    {
        private Action Unprotect { get; set; }
        public RAIIGuard(Action protect, Action unprotect)
        {
            Guard.Argument(protect, nameof(protect)).NotNull();
            Guard.Argument(unprotect, nameof(unprotect)).NotNull();

            Unprotect = unprotect;
            protect();
        }

        void IDisposable.Dispose()
        {
            Unprotect();
        }
    }
}