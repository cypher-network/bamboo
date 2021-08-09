namespace BAMWallet.Model
{
    public class TimingSettings
    {
        public uint SyncIntervalMins { get; set; } = 1;
        public uint SessionTimeoutMins { get; set; } = 30;
    }
}