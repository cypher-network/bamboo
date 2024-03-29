﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BAMWallet.Extensions
{
    public static class ByteExtensions
    {
        public static string ToBase64(this byte[] data) => Convert.ToBase64String(Encoding.UTF8.GetBytes(Byte2Hex(data)));

        public static byte[] ToByte<T>(this T arg) => Encoding.UTF8.GetBytes(arg.ToString());

        public static string ByteToHex(this byte[] data) => Byte2Hex(data);

        public static string ByteToString(this byte[] data) => Encoding.UTF8.GetString(data);

        public static string FromBytes(this byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }



        private static string Byte2Hex(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];
            int b;
            for (int i = 0; i < bytes.Length; i++)
            {
                b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c).ToLower();
        }

        public static IEnumerable<byte[]> Split(this byte[] value, int bufferLength)
        {
            int countOfArray = value.Length / bufferLength;
            if (value.Length % bufferLength > 0)
                countOfArray++;
            for (int i = 0; i < countOfArray; i++)
            {
                yield return value.Skip(i * bufferLength).Take(bufferLength).ToArray();

            }
        }

        public static bool Xor(this byte[] a, byte[] b)
        {
            int x = a.Length ^ b.Length;

            for (int i = 0; i < a.Length && i < b.Length; ++i)
            {
                x |= a[i] ^ b[i];
            }

            return x == 0;
        }

        public static byte[] WrapLengthPrefix(this byte[] message)
        {
            var lengthPrefix = BitConverter.GetBytes(message.Length);
            var ret = new byte[lengthPrefix.Length + message.Length];
            lengthPrefix.CopyTo(ret, 0);
            message.CopyTo(ret, lengthPrefix.Length);
            return ret;
        }
    }
}
