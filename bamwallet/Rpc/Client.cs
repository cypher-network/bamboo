// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.Helper;
using BAMWallet.Model;
using MessagePack;
using NitraLibSodium.Box;
using nng;
using Serilog;

namespace BAMWallet.Rpc
{
    public class Client
    {
        private readonly ILogger _logger;
        private readonly NetworkSettings _networkSettings;

        public Client(ILogger logger)
        {
            _networkSettings = Util.LiteRepositoryAppSettingsFactory().Query<NetworkSettings>().FirstOrDefault();
            _logger = logger.ForContext("SourceContext", nameof(Client));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Send<T>(params Parameter[] values)
        {
            var tcs = new TaskCompletionSource<T>();
            Task.Run(async () =>
            {
                var numberOfTriesLeft = 4;
                while (numberOfTriesLeft > 0)
                {
                    try
                    {
                        numberOfTriesLeft--;
                        if (numberOfTriesLeft == 0)
                        {
                            _logger.Here().Error("[Client - ERROR] Server seems to be offline, abandoning!");
                            tcs.SetResult(default);
                            break;
                        }

                        using var socket = NngFactorySingleton.Instance.Factory.RequesterOpen()
                            .ThenDial($"tcp://{_networkSettings.RemoteNode}").Unwrap();
                        using var ctx = socket.CreateAsyncContext(NngFactorySingleton.Instance.Factory).Unwrap();
                        var (pk, sk) = Cryptography.Crypto.KeyPair();
                        var cipher = Cryptography.Crypto.BoxSeal(MessagePackSerializer.Serialize(values),
                            _networkSettings.RemoteNodePubKey.HexToByte()[1..33]);
                        var packet = Util.Combine(pk.WrapLengthPrefix(), cipher.WrapLengthPrefix());
                        var nngMsg = NngFactorySingleton.Instance.Factory.CreateMessage();
                        nngMsg.Append(packet);
                        var nngResult = await ctx.Send(nngMsg);
                        if (!nngResult.IsOk()) continue;
                        const int prefixByteLength = 4;
                        var nngUnwrapped = nngResult.Unwrap().AsSpan().ToArray();
                        var message = Cryptography.Crypto.BoxSealOpen(
                            nngUnwrapped.Skip(prefixByteLength + (int)Box.Publickeybytes() + prefixByteLength)
                                .ToArray(), sk, pk);
                        if (message.Length == 0)
                        {
                            _logger.Here()
                                .Error(
                                    "[Client - ERROR] Cipher failed to decrypt. Please Make sure that the remote public key is correct , abandoning!");
                            tcs.SetResult(default);
                            break;
                        }

                        var data = MessagePackSerializer.Deserialize<T>(message);
                        tcs.SetResult(data);
                        break;
                    }
                    catch (NngException ex)
                    {
                        if (ex.Message != "EPROTO")
                        {
                            _logger.Here().Error(ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Here().Error(ex.Message);
                        tcs.SetResult(default);
                    }
                }
            });
            return tcs.Task.Result;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void HasRemoteAddress()
        {
            var uriString = _networkSettings.RemoteNode;
            if (!string.IsNullOrEmpty(uriString)) return;
            _logger.Here().Error("Remote node address not set in config");
            throw new Exception("Address not specified");
        }
    }
}