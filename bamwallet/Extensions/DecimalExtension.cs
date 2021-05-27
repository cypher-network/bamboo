using BAMWallet.HD;
using Dawn;

namespace BAMWallet.Extensions
{
    public static class DecimalExtension
    {
        public static ulong ConvertToUInt64(this decimal value)
        {
            Guard.Argument(value, nameof(value)).NotZero().NotNegative();
            var amount = (ulong)(value * Constant.NanoTan);
            return amount;
        }
    }
}