// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using BAMWallet.Rpc;
using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public record Parameter
    {
        [Key(0)] public byte[] Value { get; init; }
        [Key(1)] public MessageCommand MessageCommand { get; init; }
    }
}