// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

using LiteDB;

using ProtoBuf;

namespace BAMWallet.Model
{
    [ProtoContract]
    public class Transaction
    {
        [BsonId]
        public Guid Id { get; set; }
        [ProtoMember(1)]
        public byte[] TxnId { get; set; }
        [ProtoMember(2)]
        public Bp[] Bp { get; set; }
        [ProtoMember(3)]
        public int Ver { get; set; }
        [ProtoMember(4)]
        public int Mix { get; set; }
        [ProtoMember(5)]
        public Vin[] Vin { get; set; }
        [ProtoMember(6)]
        public Vout[] Vout { get; set; }
        [ProtoMember(7)]
        public RCT[] Rct { get; set; }

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

                foreach (var vin in Vin)
                {
                    ts.Append(vin.Key.K_Image);
                    ts.Append(vin.Key.K_Offsets);
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
