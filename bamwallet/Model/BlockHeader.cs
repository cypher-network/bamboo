// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public record BlockHeader
    {
        [Key(0)] public uint Version { get; init; }
        [Key(1)] public byte[] PrevBlockHash { get; init; }
        [Key(2)] public byte[] MerkleRoot { get; init; }
        [Key(3)] public ulong Height { get; init; }
        [Key(4)] public long LockTime { get; init; }
        [Key(5)] public byte[] Script { get; init; }
    }
}
