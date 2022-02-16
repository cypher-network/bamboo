// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class RCT
    {
        [Key(0)] public byte[] M { get; set; }
        [Key(1)] public byte[] P { get; set; }
        [Key(2)] public byte[] S { get; set; }
        [Key(3)] public byte[] I { get; set; }
    }
}
