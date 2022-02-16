// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using BAMWallet.Extensions;

namespace BAMWallet.Model
{
    public class Balance: IEquatable<Balance>, IComparable<Balance>
    {
        public DateTime DateTime { get; set; }
        public byte[] TxnId { get; set; }
        public ulong Total { get; set; }
        public Vout Commitment { get; set; }
        public WalletTransactionState State { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(Balance left, Balance right) => Equals(left, right);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(Balance left, Balance right) => !Equals(left, right);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is Balance balance && Equals(balance);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(TxnId.ByteToHex());
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Balance other)
        {
            return TxnId.SequenceEqual(other?.TxnId);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Balance other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var txIdComparison = string.Compare(TxnId.ByteToHex(), other.TxnId.ByteToHex(), StringComparison.Ordinal);
            return txIdComparison != 0
                ? txIdComparison
                : string.Compare(TxnId.ByteToHex(), other.TxnId.ByteToHex(), StringComparison.Ordinal);
        }
    }
}