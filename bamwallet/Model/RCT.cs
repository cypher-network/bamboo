// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using FlatSharp.Attributes;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class RCT : object
    {
        [FlatBufferItem(0)] public virtual byte[] M { get; set; }
        [FlatBufferItem(1)] public virtual byte[] P { get; set; }
        [FlatBufferItem(2)] public virtual byte[] S { get; set; }
        [FlatBufferItem(3)] public virtual byte[] I { get; set; }
    }
}
