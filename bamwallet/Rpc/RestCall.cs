// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Rpc
{
    public sealed class RestCall
    {
        public const string RestPostTransaction = "postTransaction";
        public const string RestGetTransactionId = "getTransactionId";
        public const string RestSafeguardTransactions = "getSafeguardTransactions";
        public const string RestBlocks = "getBlocks";
        public const string RestBlockHeight = "getBlockHeight";

        private readonly string _name;
        private readonly int _value;

        public static readonly RestCall PostTransaction = new RestCall(1, RestPostTransaction);
        public static readonly RestCall GetTransactionId = new RestCall(2, RestGetTransactionId);
        public static readonly RestCall GetSafeguardTransactions = new RestCall(3, RestSafeguardTransactions);
        public static readonly RestCall GetBlocks = new RestCall(4, RestBlocks);
        public static readonly RestCall GetBlockHeight = new RestCall(5, RestBlockHeight);

        private RestCall(int value, string name)
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
