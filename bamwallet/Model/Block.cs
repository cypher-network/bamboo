// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class Block
    {
        [Key(0)] public byte[] Hash { get; set; }
        [Key(1)] public ulong Height { get; set; }
        [Key(2)] public ushort Size { get; set; }
        [Key(3)] public BlockHeader BlockHeader { get; set; }
        [Key(4)] public ushort NrTx { get; set; }
        [Key(5)] public IList<Transaction> Txs { get; set; } = new List<Transaction>();
        [Key(6)] public BlockPos BlockPos { get; set; }
    }
}