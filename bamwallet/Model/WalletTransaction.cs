// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using LiteDB;

namespace BAMWallet.Model
{
    public class WalletTransaction
    {
        public ulong Balance { get; set; }
        public ulong Change { get; set; }
        public DateTime DateTime { get; set; }
        [BsonId]
        public Guid Id { get; set; }
        public string Memo { get; set; }
        public ulong Payment { get; set; }
        public string RecipientAddress { get; set; }
        public ulong Reward { get; set; }
        public string SenderAddress { get; set; }
        public bool Spent { get; set; }
        public Vout Spending { get; set; }
        public Transaction Transaction { get; set; }
        public WalletType WalletType { get; set; }
        public int Delay { get; set; }
        public bool IsVerified { get; set; }
    }
}