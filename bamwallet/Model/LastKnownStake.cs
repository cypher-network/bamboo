using System;
using LiteDB;

namespace BAMWallet.Model;

public class LastKnownStake
{
    [BsonId]
    public Guid Id { get; set; }
    public ulong Amount { get; set; }
}