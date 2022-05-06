// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Model
{
    public enum CoinType : sbyte
    {
        Empty = 0,
        Coin = 1,
        Coinbase = 2,
        Coinstake = 3,
        Fee = 4,
        Genesis = 5,
        Payment = 6,
        Change = 7,
        Timebase = 8
    }
}
