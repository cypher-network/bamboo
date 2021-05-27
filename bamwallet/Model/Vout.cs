// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class Vout
    {
        [Key(0)] public ulong A { get; set; }
        [Key(1)] public byte[] C { get; set; }
        [Key(2)] public byte[] E { get; set; }
        [Key(3)] public long L { get; set; }
        [Key(4)] public byte[] N { get; set; }
        [Key(5)] public byte[] P { get; set; }
        [Key(6)] public string S { get; set; }
        [Key(7)] public CoinType T { get; set; }
    }
}
