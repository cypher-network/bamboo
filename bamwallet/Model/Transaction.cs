// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using BAMWallet.Extensions;
using BAMWallet.Helper;
using Blake3;
using LiteDB;
using MessagePack;
using NBitcoin;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class Transaction
    {
        [IgnoreMember] [BsonId] public Guid Id { get; set; }
        [Key(0)] public byte[] TxnId { get; set; }
        [Key(1)] public Bp[] Bp { get; set; }
        [Key(2)] public int Ver { get; set; }
        [Key(3)] public int Mix { get; set; }
        [Key(4)] public Vin[] Vin { get; set; }
        [Key(5)] public Vout[] Vout { get; set; }
        [Key(6)] public RCT[] Rct { get; set; }
        [Key(7)] public Vtime Vtime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToHash()
        {
            return Hasher.Hash(ToStream()).HexToByte();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToStream()
        {
            using var ts = new TangramStream();
            ts
                .Append(Mix)
                .Append(Ver);

            foreach (var bp in Bp)
            {
                ts.Append(bp.Proof);
            }

            foreach (var input in Vin)
            {
                ts.Append(input.Key.KImage);
                ts.Append(input.Key.KOffsets);
            }

            foreach (var output in Vout)
            {
                ts
                    .Append(output.A)
                    .Append(output.C)
                    .Append(output.E)
                    .Append(output.L)
                    .Append(output.N)
                    .Append(output.P)
                    .Append(output.S ?? string.Empty)
                    .Append(output.T.ToString());
            }

            foreach (var rct in Rct)
            {
                ts
                    .Append(rct.I)
                    .Append(rct.M)
                    .Append(rct.P)
                    .Append(rct.S);
            }

            if (Vtime != null)
            {
                ts
                    .Append(Vtime.I)
                    .Append(Vtime.L)
                    .Append(Vtime.M)
                    .Append(Vtime.N)
                    .Append(Vtime.S)
                    .Append(Vtime.W);
            }

            return ts.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            return MessagePackSerializer.Serialize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <param name="scan"></param>
        /// <returns></returns>
        public static WalletTransactionMessage Message(Vout output, Key scan)
        {
            WalletTransactionMessage message = null;

            try
            {
                message = MessagePackSerializer.Deserialize<WalletTransactionMessage>(scan.Decrypt(output.N));
                message.Output = output;
            }
            catch (Exception)
            {
                // Ignore
            }

            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <param name="scan"></param>
        /// <returns></returns>
        public static ulong Amount(Vout output, Key scan)
        {
            ulong amount = 0;

            try
            {
                amount = MessagePackSerializer.Deserialize<WalletTransactionMessage>(scan.Decrypt(output.N)).Amount;
            }
            catch (Exception ex)
            {
                var e = ex.Message;
                // Ignore
            }

            return amount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <param name="scan"></param>
        /// <returns></returns>
        public static string Memo(Vout output, Key scan)
        {
            var message = string.Empty;

            try
            {
                message = MessagePackSerializer.Deserialize<WalletTransactionMessage>(scan.Decrypt(output.N)).Memo;
            }
            catch (Exception)
            {
                // Ignore
            }

            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="blind"></param>
        /// <param name="memo"></param>
        /// <returns></returns>
        public static byte[] Message(ulong amount, ulong paid, byte[] blind, string memo)
        {
            return MessagePackSerializer.Serialize(new WalletTransactionMessage
            {
                Amount = amount,
                Blind = blind,
                Memo = memo,
                Date = DateTime.UtcNow,
                Paid = paid
            });
        }
    }
}
