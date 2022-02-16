// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.HD
{
    public class Network
    {
        private readonly string _name;
        private readonly int _value;

        public static readonly Network Mainnet = new Network(1, Constant.Mainnet);
        public static readonly Network Testnet = new Network(2, Constant.Testnet);

        private Network(int value, string name)
        {
            _value = value;
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
