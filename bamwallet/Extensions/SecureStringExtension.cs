using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace BAMWallet.Extensions
{
    public static class SecureStringExtension
    {
        public static string ToUnSecureString(this SecureString secureString)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        public static byte[] ToArray(this SecureString s)
        {
            if (s == null)
                throw new NullReferenceException();

            if (s.Length == 0)
                return Array.Empty<byte>();

            var result = new List<byte>();
            var ptr = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);

            try
            {
                var i = 0;
                do
                {
                    var b = Marshal.ReadByte(ptr, i++);
                    if (b == 0)
                        break;

                    result.Add(b);

                } while (true);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
            return result.ToArray();
        }
    }
}
