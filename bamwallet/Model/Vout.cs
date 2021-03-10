// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using FlatSharp.Attributes;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class Vout : object
    {
        [FlatBufferItem(0)] public virtual ulong A { get; set; }
        [FlatBufferItem(1)] public virtual byte[] C { get; set; }
        [FlatBufferItem(2)] public virtual byte[] E { get; set; }
        [FlatBufferItem(3)] public virtual long L { get; set; }
        [FlatBufferItem(4)] public virtual byte[] N { get; set; }
        [FlatBufferItem(5)] public virtual byte[] P { get; set; }
        [FlatBufferItem(6)] public virtual string S { get; set; }
        [FlatBufferItem(7, DefaultValue = CoinType.Coin)] public virtual CoinType T { get; set; }
    }
}
