﻿// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class Payment
    {
        [Key(0)] public ulong Amount { get; set; }
        [Key(1)] public string Address { get; set; }
        [Key(2)] public Credentials Credentials { get; set; }
        [Key(3)] public ulong Reward { get; set; }
        [Key(4)] public string Memo { get; set; }
        [Key(5)] public SessionType SessionType { get; set; }
        [Key(6)] public bool Recover { get; set; }
        [Key(7)] public ulong BlockHeight { get; set; }
    }
}
