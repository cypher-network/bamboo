// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Newtonsoft.Json;

using LiteDB;

using FlatSharp;

using BAMWallet.Model;
using BAMWallet.Extentions;
using NBitcoin;

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

            if (!Directory.Exists(wallets))
            {
                try
                {
                    Directory.CreateDirectory(wallets);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }

            return wallet;
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

        public static byte[] SerializeFlatBuffer<T>(T data) where T : class
        {
            try
            {
                int maxSize = FlatBufferSerializer.Default.GetMaxSize(data);
                byte[] buffer = new byte[maxSize];
                FlatBufferSerializer.Default.Serialize(data, buffer);
                return buffer;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T DeserializeFlatBuffer<T>(byte[] data) where T : class
        {
            try
            {
                return FlatBufferSerializer.Default.Parse<T>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static byte[] ReadFully(Stream input)
        {
            using var ms = new MemoryStream();

            input.CopyTo(ms);
            return ms.ToArray();
        }

        public static WalletTransactionMessage Message(Vout vout, Key scan)
        {
            WalletTransactionMessage message = null;

            try
            {
                message = DeserializeFlatBuffer<WalletTransactionMessage>(scan.Decrypt(vout.N));
            }
            catch
            {
                throw;
            }

            return message;
        }

        public static ulong MessageAmount(Vout vout, Key scan)
        {
            ulong amount = 0;

            try
            {
                amount = DeserializeFlatBuffer<WalletTransactionMessage>(scan.Decrypt(vout.N)).Amount;
            }
            catch
            {
                throw;
            }

            return amount;
        }

        public static string MessageMemo(Vout vout, Key scan)
        {
            string message = string.Empty;

            try
            {
                message = DeserializeFlatBuffer<WalletTransactionMessage>(scan.Decrypt(vout.N)).Memo;
            }
            catch
            {
                throw;
            }

            return message;
        }

        public static byte[] Message(ulong amount, byte[] blind, string memo)
        {
            return SerializeFlatBuffer(new WalletTransactionMessage { Amount = amount, Blind = blind, Memo = memo });
        }
    }
}
