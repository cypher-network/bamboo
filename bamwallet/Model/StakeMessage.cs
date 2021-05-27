// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class StakeMessage
    {
        [Key(0)]
        public ulong Node { get; set; }
        [Key(1)]
        public byte[] Payload { get; set; }
        [Key(2)]
        public byte[] PublicKey { get; set; }
        [Key(3)]
        public byte[] Signature { get; set; }
        [Key(4)]
        public string Message { get; set; }
        [Key(5)]
        public bool Error { get; set; }
    }
}
