// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using FlatSharp.Attributes;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class Credentials : object
    {
        [FlatBufferItem(0)] public virtual string Identifier { get; set; }
        [FlatBufferItem(1)] public virtual string Passphrase { get; set; }
    }
}
