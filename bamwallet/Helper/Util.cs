// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Security;
using LiteDB;
using BAMWallet.Extensions;

namespace BAMWallet.Helper
{
    public static class Util
    {
        public static string AppDomainDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string WalletPath(string id)
        {
            var wallets = Path.Combine(Path.GetDirectoryName(AppDomainDirectory()), "wallets");
            var wallet = Path.Combine(wallets, $"{id}.db");

            if (Directory.Exists(wallets)) return wallet;
            try
            {
                Directory.CreateDirectory(wallets);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return wallet;
        }

        public static string GetAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var rng = new Random();
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static LiteRepository LiteRepositoryFactory(SecureString identifier, SecureString passphrase)
        {
            var connectionString = new ConnectionString
            {
                Filename = WalletPath(identifier.ToUnSecureString()),
                Password = passphrase.ToUnSecureString(),
                Connection = ConnectionType.Shared
            };

            return new LiteRepository(connectionString);
        }

        public static ulong Sum(IEnumerable<ulong> source)
        {
            return source.Aggregate(0UL, (current, number) => current + number);
        }

        public static byte[] StreamToArray(Stream input)
        {
            using var ms = new MemoryStream();

            input.CopyTo(ms);
            return ms.ToArray();
        }

        public static BigInteger Mod(BigInteger a, BigInteger n)
        {
            var result = a % n;
            if (result < 0 && n > 0 || result > 0 && n < 0)
            {
                result += n;
            }

            return result;
        }

        public static DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }

        public static DateTime GetAdjustedTime()
        {
            return GetUtcNow().Add(TimeSpan.Zero);
        }

        public static long GetAdjustedTimeAsUnixTimestamp()
        {
            return new DateTimeOffset(GetAdjustedTime()).ToUnixTimeSeconds();
        }
    }
}
