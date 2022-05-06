// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public record Block
    {
        [Key(0)] public byte[] Hash { get; init; }
        [Key(1)] public ulong Height { get; init; }
        [Key(2)] public ushort Size { get; init; }
        [Key(3)] public BlockHeader BlockHeader { get; init; }
        [Key(4)] public ushort NrTx { get; init; }
        [Key(5)] public IList<Transaction> Txs { get; init; }
        [Key(6)] public BlockPPoS BlockPos { get; init; }
    }
}