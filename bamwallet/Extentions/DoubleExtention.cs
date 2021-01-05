using System;

using Dawn;

using BAMWallet.HD;

namespace BAMWallet.Extentions
{
    public static class DoubleExtention
    {

        //    public static ulong ConvertToUInt64(this double value)
        //    {
        //        Guard.Argument(value, nameof(value)).NotZero().NotNegative();

        //        ulong amount;

        //        try
        //        {
        //            var parts = value.ToString().Split(new char[] { '.', ',' });
        //            var part1 = (ulong)System.Math.Truncate(value);

        //            if (parts.Length.Equals(1))
        //                amount = part1.MulWithNaT();
        //            else
        //            {
        //                var part2 = (ulong)((value - part1) * ulong.Parse("1".PadRight(parts[1].Length + 1, '0')) + 0.5);
        //                amount = part1.MulWithNaT() + ulong.Parse(part2.ToString());
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }

        //        return amount;
        //    }

        //    public static byte[] ToBytes<T>(this T arg) => Encoding.UTF8.GetBytes(arg.ToString());
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ulong ConvertToUInt64(this double value)
        {
            Guard.Argument(value, nameof(value)).NotZero().NotNegative();

            ulong amount;

            try
            {
                amount = (ulong)(value * Constant.NanoTan);
            }
            catch (Exception)
            {
                throw;
            }

            return amount;
        }
    }
}
