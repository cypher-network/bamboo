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

using ProtoBuf;

using BAMWallet.Model;
using BAMWallet.Extentions;
using NBitcoin;

namespace BAMWallet.Helper
{
    public static class Util
    {
        internal static Random _random = new Random();

        public static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public static OSPlatform GetOSPlatform()
        {
            OSPlatform osPlatform = OSPlatform.Create("Other Platform");
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            osPlatform = isWindows ? OSPlatform.Windows : osPlatform;

            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            osPlatform = isOSX ? OSPlatform.OSX : osPlatform;

            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            osPlatform = isLinux ? OSPlatform.Linux : osPlatform;

            return osPlatform;
        }

        public static string AppDomainDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static Stream TangramData(string id)
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

            return File.Open(wallet, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
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

        public static LiteRepository LiteRepositoryFactory(SecureString secret, string identifier)
        {
            var connectionString = new ConnectionString
            {
                Filename = WalletPath(identifier),
                Password = secret.ToUnSecureString()
            };

            return new LiteRepository(connectionString);
        }

        public static string ToPlainString(SecureString secure)
        {
            return new NetworkCredential(string.Empty, secure).Password;
        }

        public static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using var sr = new StreamReader(stream);
            using var jtr = new JsonTextReader(sr);
            try
            {
                var js = new Newtonsoft.Json.JsonSerializer();
                var searchResult = js.Deserialize<T>(jtr);
                return searchResult;
            }
            catch (JsonSerializationException)
            {
                throw;
            }
        }

        public static IEnumerable<T> DeserializeJsonEnumerable<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using (var sr = new StreamReader(stream))
            using (var jtr = new JsonTextReader(sr))
            {
                try
                {
                    var js = new Newtonsoft.Json.JsonSerializer();
                    var searchResult = js.Deserialize<IEnumerable<T>>(jtr);
                    return searchResult;
                }
                catch (JsonSerializationException)
                {
                    throw;
                }

            }
        }

        public static async Task<string> StreamToStringAsync(Stream stream)
        {
            string content = null;

            if (stream != null)
                using (var sr = new StreamReader(stream))
                    content = await sr.ReadToEndAsync();

            return content;
        }

        public static string GetFileHash(FileInfo file)
        {
            return GetFileHash(file.FullName);
        }

        public static string GetFileHash(string fileFullName)
        {
            var bytes = File.ReadAllBytes(fileFullName);
            var hash = SHA384ManagedHash(bytes);

            return hash.ByteToHex();
        }

        public static byte[] SHA384ManagedHash(byte[] data)
        {
            SHA384 sHA384 = new SHA384Managed();
            return sHA384.ComputeHash(data);
        }

        [CLSCompliant(false)]
#pragma warning disable CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
        public static InsecureString Insecure(this SecureString secureString) => new InsecureString(secureString);
#pragma warning restore CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute

        public async static Task<T> TriesUntilCompleted<T>(Func<Task<T>> action, int tries, int delay, T expected)
        {
            var result = default(T);

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    result = await action();
                    if (result.Equals(expected))
                        break;
                }
                finally
                {
                    await Task.Delay(delay);
                }
            }

            return result;
        }

        public async static Task<TaskResult<T>> TriesUntilCompleted<T>(Func<Task<TaskResult<T>>> action, int tries, int delay) where T : class
        {
            var result = default(TaskResult<T>);

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    result = await action();
                    if (result.Result != null)
                        break;
                }
                finally
                {
                    await Task.Delay(delay);
                }
            }

            return result;
        }

        public static ulong Sum(IEnumerable<ulong> source)
        {
            var sum = 0UL;
            foreach (var number in source)
            {
                sum += number;
            }
            return sum;
        }

        public static byte[] SerializeProto<T>(T data)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, data);
                    return ms.ToArray();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T DeserializeProto<T>(byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    return Serializer.Deserialize<T>(ms);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static IEnumerable<T> DeserializeListProto<T>(byte[] data) where T : class
        {
            List<T> list = new List<T>();

            try
            {
                using (var ms = new MemoryStream(data))
                {
                    T item;
                    while ((item = Serializer.DeserializeWithLengthPrefix<T>(ms, PrefixStyle.Base128, fieldNumber: 1)) != null)
                    {
                        list.Add(item);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return list.AsEnumerable();
        }

        public static unsafe byte[] GetBytes(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            if (str.Length == 0) return new byte[0];

            fixed (char* p = str)
            {
                return new Span<byte>(p, str.Length * sizeof(char)).ToArray();
            }
        }

        public static unsafe string GetString(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length % sizeof(char) != 0) throw new ArgumentException($"Invalid {nameof(bytes)} length");
            if (bytes.Length == 0) return string.Empty;

            fixed (byte* p = bytes)
            {
                return new string(new Span<char>(p, bytes.Length / sizeof(char)));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public static byte[] SHA256ManagedHash(byte[] data)
        {
            SHA256Managed hasher = new System.Security.Cryptography.SHA256Managed();
            return hasher.ComputeHash(data);
        }

        private static Random rng = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static byte[] ReadFully(Stream input)
        {
            using MemoryStream ms = new MemoryStream();

            input.CopyTo(ms);
            return ms.ToArray();
        }

        public static WalletTransactionMessage Message(Vout vout, Key scan)
        {
            WalletTransactionMessage message = null;

            try
            {
                message = DeserializeProto<WalletTransactionMessage>(scan.Decrypt(vout.N));
            }
            catch
            { }

            return message;
        }

        public static ulong MessageAmount(Vout vout, Key scan)
        {
            ulong amount = 0;

            try
            {
                amount = DeserializeProto<WalletTransactionMessage>(scan.Decrypt(vout.N)).Amount;
            }
            catch
            { }

            return amount;
        }

        public static string MessageMemo(Vout vout, Key scan)
        {
            string message = string.Empty;

            try
            {
                message = DeserializeProto<WalletTransactionMessage>(scan.Decrypt(vout.N)).Memo;
            }
            catch
            { }

            return message;
        }

        public static byte[] Message(ulong amount, byte[] blind, string memo)
        {
            return SerializeProto(new WalletTransactionMessage { Amount = amount, Blind = blind, Memo = memo });
        }
    }
}
