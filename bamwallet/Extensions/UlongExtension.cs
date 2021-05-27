using System;

using BAMWallet.HD;

namespace BAMWallet.Extensions
{
    public static class UlongExtension
    {
        public static ulong MulWithNaT(this ulong value) => value * Constant.NanoTan;

        public static decimal DivWithNaT(this ulong value) => Convert.ToDecimal(value) / Constant.NanoTan;
    }
}
