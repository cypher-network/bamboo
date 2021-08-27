using System;

namespace CLi.ApplicationLayer.Events
{
    public class SyncStateChanged : EventArgs
    {
        public enum SyncState
        {
            Idle,
            SyncInProgress
        };
        public SyncState SyncStatus { get; }

        public SyncStateChanged(SyncState state)
        {
            SyncStatus = state;
        }
    };
}