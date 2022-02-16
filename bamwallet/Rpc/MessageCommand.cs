// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Rpc
{
    public enum MessageCommand : byte
    {
        GetBlocks = 0x14,
        GetBlockCount = 0x18,
        GetMemTransaction = 0x19,
        GetTransaction = 0x20,
        Transaction = 0x21,
        GetSafeguardBlocks = 0x23,
        GetPosTransaction = 0x24,
        GetTransactionBlockIndex = 0x25,
        Stake = 0x26,
    }
}