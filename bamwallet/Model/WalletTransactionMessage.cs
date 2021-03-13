// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using FlatSharp.Attributes;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class WalletTransactionMessage : object
    {
        [FlatBufferItem(1)]
        public ulong Amount { get; set; }
        [FlatBufferItem(2)]
        public byte[] Blind { get; set; }
        [FlatBufferItem(3)]
        public string Memo { get; set; }
    }
}
