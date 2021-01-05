// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using LiteDB;

namespace BAMWallet.Model
{
    public class KeySet
    {
        public string ChainCode { get; set; }
        public string KeyPath { get; set; }
        public string RootKey { get; set; }
        [BsonId]
        public string StealthAddress { get; set; }
    }
}