// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using FlatSharp.Attributes;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class BlockHeader : object
    {
        [FlatBufferItem(0)] public virtual int Bits { get; set; }
        [FlatBufferItem(1)] public virtual long Height { get; set; }
        [FlatBufferItem(2)] public virtual long Locktime { get; set; }
        [FlatBufferItem(3)] public virtual string LocktimeScript { get; set; }
        [FlatBufferItem(4)] public virtual string MerkelRoot { get; set; }
        [FlatBufferItem(5)] public virtual string Nonce { get; set; }
        [FlatBufferItem(6)] public virtual string PrevMerkelRoot { get; set; }
        [FlatBufferItem(7)] public virtual string Proof { get; set; }
        [FlatBufferItem(8)] public virtual string Sec { get; set; }
        [FlatBufferItem(9)] public virtual string Seed { get; set; }
        [FlatBufferItem(10)] public virtual ulong Solution { get; set; }
        [FlatBufferItem(11)] public virtual Transaction[] Transactions { get; set; }
        [FlatBufferItem(12)] public virtual int Version { get; set; }
        [FlatBufferItem(13)] public virtual string VrfSig { get; set; }
    }
}
