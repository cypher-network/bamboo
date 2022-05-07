// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using MessagePack;

namespace BAMWallet.Model
{

    [MessagePackObject]
    public record TransactionBlockIndexResponse([property: Key(0)] ulong Index);
    public record TransactionBlockIndexRequest(byte[] TransactionId);

    [MessagePackObject]
    public record BlockCountResponse([property: Key(0)] long Count);

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
    /// <param name="Blocks"></param>
    /// <param name="Error"></param>
    [MessagePackObject]
    public record SafeguardBlocksResponse([property: Key(0)] IReadOnlyList<Block> Blocks, [property: Key(1)] string Error);
    public record SafeguardBlocksRequest(int NumberOfBlocks);

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
        [Key(0)] public string Message { get; init; }
        [Key(1)] public bool Success { get; init; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record StakeCredentialsRequest
    {
        [Key(0)] public byte[] Seed { get; init; }
        [Key(1)] public byte[] Passphrase { get; init; }
        [Key(2)] public byte[] RewardAddress { get; init; }
        [Key(3)] public Output[] Outputs { get; init; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Value"></param>
    /// <typeparam name="T"></typeparam>
    public record MessageResponse<T>(T Value);
}