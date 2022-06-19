// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using LiteDB;

namespace BAMWallet.Model
{
    public class NetworkSettings
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Environment { get; set; }
        public string WalletEndpoint { get; set; }
        public string RemoteNode { get; set; }
        public int RemoteNodeHttpPort { get; set; }
        public string RemoteNodePubKey { get; set; }
        public ulong NumberOfConfirmations { get; set; }
    }
}