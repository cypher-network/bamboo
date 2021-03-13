// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using FlatSharp.Attributes;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class GenericList<T> : object
    {
        [FlatBufferItem(0)]
        public virtual IList<T> Data { get; set; } = new List<T>();
    }
}