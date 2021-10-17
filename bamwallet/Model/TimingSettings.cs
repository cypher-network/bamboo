namespace BAMWallet.Model
{
    public class TimingSettings
    {
        public uint SyncIntervalSecs { get; set; } = 60;
        public uint SessionTimeoutMins { get; set; } = 30;
    }
}