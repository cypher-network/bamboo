using System;

using BAMWallet.HD;

namespace BAMWallet.Extensions
{
    public static class UlongExtension
    {
        public static ulong MulWithGYin(this ulong value) => value * Constant.GYin;

        public static decimal DivWithGYin(this ulong value) => Convert.ToDecimal(value) / Constant.GYin;
    }
}
