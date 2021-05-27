// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class BlockHeader
    {
        [Key(0)] public int Bits { get; set; }
        [Key(1)] public long Height { get; set; }
        [Key(2)] public long Locktime { get; set; }
        [Key(3)] public string LocktimeScript { get; set; }
        [Key(4)] public string MerkelRoot { get; set; }
        [Key(5)] public string Nonce { get; set; }
        [Key(6)] public string PrevMerkelRoot { get; set; }
        [Key(7)] public string Proof { get; set; }
        [Key(8)] public string Sec { get; set; }
        [Key(9)] public string Seed { get; set; }
        [Key(10)] public ulong Solution { get; set; }
        [Key(11)] public Transaction[] Transactions { get; set; }
        [Key(12)] public int Version { get; set; }
        [Key(13)] public string VrfSig { get; set; }
    }
}
