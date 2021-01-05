using System;

using BAMWallet.HD;

namespace BAMWallet.Extentions
{
    public static class UlongExtention
    {
        public static ulong MulWithNaT(this ulong value) => value * Constant.NanoTan;

        public static double DivWithNaT(this ulong value) => Convert.ToDouble(value) / Constant.NanoTan;
    }
}
