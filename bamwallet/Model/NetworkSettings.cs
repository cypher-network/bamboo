namespace BAMWallet.Model
{
    public class NetworkSettings
    {
        public string Environment { get; set; }
        public bool RunAsWebServer { get; set; }
        public string WalletEndpoint { get; set; }
        public string WebserverEndpoint { get; set; }
        public string RemoteNode { get; set; }
        public uint NumberOfConfirmations { get; set; }
        public Routing Routing { get; set; }
    }

    public class Routing
    {
        public string Transaction { get; set; }
        public string TransactionId { get; set; }
        public string MempoolTransactionId { get; set; }
        public string SafeguardTransactions { get; set; }
        public string Blocks { get; set; }
        public string BlockHeight { get; set; }
    }
}