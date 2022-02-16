﻿// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class KeyOffsetImage
    {
        [Key(0)] public byte[] KImage { get; set; }
        [Key(1)] public byte[] KOffsets { get; set; }
    }
}
