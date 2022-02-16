// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Model
{
    public class Balance
    {
        public ulong Total { get; set; }
        public Vout Commitment { get; set; }
    }
}