// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;

using ProtoBuf;

namespace BAMWallet.Model
{
    [ProtoContract]
    public class BlockHeader
    {
        [ProtoMember(1)]
        public int Bits { get; set; }
        [ProtoMember(2)]
        public string MrklRoot { get; set; }
        [ProtoMember(3)]
        public string Nonce { get; set; }
        [ProtoMember(4)]
        public string PrevMrklRoot { get; set; }
        [ProtoMember(5)]
        public string Proof { get; set; }
        [ProtoMember(6)]
        public string SecKey256 { get; set; }
        [ProtoMember(7)]
        public string Seed { get; set; }
        [ProtoMember(8)]
        public ulong Solution { get; set; }
        [ProtoMember(9)]
        public long Locktime { get; set; }
        [ProtoMember(10)]
        public string LocktimeScript { get; set; }
        [ProtoMember(11)]
        public HashSet<Transaction> Transactions { get; set; }
        [ProtoMember(12)]
        public int Version { get; set; }
        [ProtoMember(13)]
        public string VrfSig { get; set; }
    }
}
