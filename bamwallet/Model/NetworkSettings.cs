// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Model
{
    public class NetworkSettings
    {
        public string Environment { get; set; }
        public string WalletEndpoint { get; set; }
        public string RemoteNode { get; set; }
        public string RemoteNodePubKey { get; set; }
        public ulong NumberOfConfirmations { get; set; }
        public bool RunSilently { get; set; }
        public bool Staking { get; set; }
        public string StakeListening { get; set; }
    }
}