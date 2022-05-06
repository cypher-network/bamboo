// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model;

[MessagePackObject]
public record Output 
{
    [Key(0)] public byte[] C { get; set; }
    [Key(1)] public byte[] E { get; set; }
    [Key(2)] public byte[] N { get; set; }
    [Key(3)] public CoinType T { get; set; }
}