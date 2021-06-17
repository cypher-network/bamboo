// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class BlockHeader
    {
        [Key(0)] public uint Version { get; set; }
        [Key(1)] public byte[] PrevBlockHash { get; set; }
        [Key(2)] public byte[] MerkleRoot { get; set; }
        [Key(3)] public ulong Height { get; set; }
        [Key(4)] public long Locktime { get; set; }
        [Key(5)] public string LocktimeScript { get; set; }
    }
}
