// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Model
{
    public class BalanceSheet
    {
        public string Date { get; set; }
        public string Memo { get; set; }
        public string MoneyOut { get; set; }
        public string Fee { get; set; }
        public string MoneyIn { get; set; }
        public string Reward { get; set; }
        public string Balance { get; set; }
        public Vout[] Outputs { get; set; }
        public string TxId { get; set; }
    }
}