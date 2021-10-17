// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
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
        [Key(0)] public bool OK { get; set; }
    }
    
    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record PoSPoolTransactionResponse
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
    }
    
    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record StakeRequest
    {
        [Key(0)] public Payment Payment { get; set; }
    }
    
    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record StakeResponse
    {
        [Key(0)] public Transaction Transaction { get; init; }
        [Key(1)] public string Message { get; init; }
    }
    
    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public record TransactionResponse
    {
        [Key(0)] public Transaction Transaction { get; set; }
    }
}