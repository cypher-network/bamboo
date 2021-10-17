// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using MessagePack;
using NBitcoin;

namespace BAMWallet.Model
{
    [MessagePackObject]
    public class Vout
    {
        [Key(0)] public ulong A { get; set; }
        [Key(1)] public byte[] C { get; set; }
        [Key(2)] public byte[] E { get; set; }
        [Key(3)] public long L { get; set; }
        [Key(4)] public byte[] N { get; set; }
        [Key(5)] public byte[] P { get; set; }
        [Key(6)] public string S { get; set; }
        [Key(7)] public CoinType T { get; set; }
        [Key(8)] public byte[] D { get; set; }
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public bool IsLockedOrInvalid()
        {
            if (T != CoinType.Coinbase) return false;

            var lockTime = new LockTime(Helper.Util.UnixTimeToDateTime(L));
            var script = S;
            var sc1 = new Script(Op.GetPushOp(lockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY);
            var sc2 = new Script(script);
            if (!sc1.ToString().Equals(sc2.ToString()))
            {
                return true;
            }

            var tx = Network.Main.CreateTransaction();
            tx.Outputs.Add(new TxOut { ScriptPubKey = new Script(script) });
            var spending = Network.Main.CreateTransaction();
            spending.LockTime = new LockTime(DateTimeOffset.UtcNow);
            spending.Inputs.Add(new TxIn(tx.Outputs.AsCoins().First().Outpoint, new Script()));
            spending.Inputs[0].Sequence = 1;
            return !spending.Inputs.AsIndexedInputs().First().VerifyScript(tx.Outputs[0]);
        }
    }
}
