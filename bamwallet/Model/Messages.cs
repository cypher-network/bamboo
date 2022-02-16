// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using MessagePack;

namespace BAMWallet.Model
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Index"></param>
    [MessagePackObject(true)]
    public record TransactionBlockIndexResponse(ulong Index);
    public record TransactionBlockIndexRequest(byte[] TransactionId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Count"></param>
    [MessagePackObject(true)]
    public record BlockCountResponse(long Count);

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record BlocksResponse
    {
        [Key(0)] public List<Block> Blocks { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record MemoryPoolTransactionResponse
    {
        [Key(0)] public Transaction Transaction { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record NewTransactionResponse
    {
        [Key(0)] public bool Ok { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record PosPoolTransactionResponse
    {
        [Key(0)] public Transaction Transaction { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record SafeguardBlocksResponse
    {
        [Key(0)] public IList<Block> Blocks { get; set; }
        [Key(1)] public string Error { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record TransactionResponse
    {
        [Key(0)] public Transaction Transaction { get; set; }
    }
    
    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record StakeResponse(int Code);

    [MessagePackObject]
    public record StakeRequest
    {
        [Key(0)] public byte[] Tag { get; set; }
        [Key(1)] public byte[] Nonce { get; set; }
        [Key(2)] public byte[] Token { get; set; }
        [Key(3)] public byte[] Data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record StakeCredentialsResponse
    {
        [Key(0)] public bool Success { get; init; }
        [Key(1)] public string Message { get; init; }
    }

    [MessagePackObject]
    public record StakeCredentialsRequest
    {
        [Key(0)] public byte[] Seed { get; init; }
        [Key(1)] public byte[] Passphrase { get; init; }
        [Key(2)] public byte[] RewardAddress { get; init; }
        [Key(3)] public Transaction[] Transactions { get; set; }
    }

    public record MessageResponse<T>(T Value);
}