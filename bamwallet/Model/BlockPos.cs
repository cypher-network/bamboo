// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class BlockPos
    {
        [Key(0)] public uint Bits { get; set; }
        [Key(1)] public ulong Solution { get; set; }
        [Key(2)] public byte[] Nonce { get; set; }
        [Key(3)] public byte[] VrfProof { get; set; }
        [Key(4)] public byte[] VrfSig { get; set; }
        [Key(5)] public byte[] PublicKey { get; set; }

    }
}