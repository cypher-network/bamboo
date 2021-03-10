// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using FlatSharp.Attributes;
using LiteDB;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class Transaction : object
    {
        [BsonId] public Guid Id { get; set; }
        [FlatBufferItem(0)] public virtual byte[] TxnId { get; set; }
        [FlatBufferItem(1)] public virtual Bp[] Bp { get; set; }
        [FlatBufferItem(2)] public virtual int Ver { get; set; }
        [FlatBufferItem(3)] public virtual int Mix { get; set; }
        [FlatBufferItem(4)] public virtual Vin[] Vin { get; set; }
        [FlatBufferItem(5)] public virtual Vout[] Vout { get; set; }
        [FlatBufferItem(6)] public virtual RCT[] Rct { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToHash()
        {
            return NBitcoin.Crypto.Hashes.DoubleSHA256(Stream()).ToBytes(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Stream()
        {
            byte[] stream;
            using (var ts = new Helper.TangramStream())
            {
                ts
                .Append(TxnId ?? Array.Empty<byte>())
                .Append(Mix)
                .Append(Ver);

                foreach (var bp in Bp)
                {
                    ts.Append(bp.Proof);
                }

                foreach (var vin in Vin)
                {
                    ts.Append(vin.Key.KImage);
                    ts.Append(vin.Key.KOffsets);
                }

                foreach (var vout in Vout)
                {
                    ts
                      .Append(vout.A)
                      .Append(vout.C)
                      .Append(vout.E)
                      .Append(vout.L)
                      .Append(vout.N)
                      .Append(vout.P)
                      .Append(vout.S ?? string.Empty)
                      .Append(vout.T.ToString());
                }

                foreach (var rct in Rct)
                {
                    ts
                      .Append(rct.I)
                      .Append(rct.M)
                      .Append(rct.P)
                      .Append(rct.S);
                }

                stream = ts.ToArray();
            }

            return stream;
        }
    }
}
