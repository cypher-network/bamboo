// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public record BlockPPoS
    {
        [Key(0)] public uint Bits { get; init; }
        [Key(1)] public ulong Solution { get; init; }
        [Key(2)] public byte[] Nonce { get; init; }
        [Key(3)] public byte[] VrfProof { get; init; }
        [Key(4)] public byte[] VrfSig { get; init; }
        [Key(5)] public byte[] PublicKey { get; init; }

    }
}