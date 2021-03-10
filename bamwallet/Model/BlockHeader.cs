// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using FlatSharp.Attributes;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class BlockHeader
    {
        [FlatBufferItem(0)] public int Bits { get; set; }
        [FlatBufferItem(1)] public long Height { get; set; }
        [FlatBufferItem(2)] public long Locktime { get; set; }
        [FlatBufferItem(3)] public string LocktimeScript { get; set; }
        [FlatBufferItem(4)] public string MerkelRoot { get; set; }
        [FlatBufferItem(5)] public string Nonce { get; set; }
        [FlatBufferItem(6)] public string PrevMerkelRoot { get; set; }
        [FlatBufferItem(7)] public string Proof { get; set; }
        [FlatBufferItem(8)] public string Sec { get; set; }
        [FlatBufferItem(9)] public string Seed { get; set; }
        [FlatBufferItem(10)] public ulong Solution { get; set; }
        [FlatBufferItem(11)] public Transaction[] Transactions { get; set; }
        [FlatBufferItem(12)] public int Version { get; set; }
        [FlatBufferItem(13)] public string VrfSig { get; set; }
    }
}
