using System;
using LiteDB;

namespace BAMWallet.Model;

public class AddressBook
{
    [BsonId]
    public Guid Id { get; set; }
    public DateTime Created { get; set; }
    public string RecipientAddress { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}