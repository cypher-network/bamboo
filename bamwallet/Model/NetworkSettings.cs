namespace BAMWallet.Model
{
    public class NetworkSettings
    {
        public string Environment { get; set; }
        public string WalletEndpoint { get; set; }
        public string RemoteNode { get; set; }
        public uint NumberOfConfirmations { get; set; }
        public bool RunSilently { get; set; }
        public bool Staking { get; set; }
        public string StakeListening { get; set; }
    }
}