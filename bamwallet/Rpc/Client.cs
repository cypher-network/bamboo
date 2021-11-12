// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Threading;
using BAMWallet.Extensions;
using BAMWallet.Helper;
using MessagePack;
using BAMWallet.Model;
using NetMQ;
using NetMQ.Sockets;
using Serilog;

namespace BAMWallet.Rpc
{
    public class Client : IDisposable
    {
        // This should increase when running a vpn, tor or i2p ----------------+
        private const int SocketTryReceiveFromMilliseconds = 2500;          // +
        // +-------------------------------------------------------------------+
        
        private int _numberOfTriesLeft = 4;
        private DealerSocket _dealerSocket;
        
        private readonly ILogger _logger;
        private readonly NetworkSettings _networkSettings;

        public Client(NetworkSettings networkSettings, ILogger logger)
        {
            _networkSettings = networkSettings;
            _logger = logger.ForContext("SourceContext", nameof(Client));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Send<T>(MessageCommand command, params Parameter[] values)
        {
            T value = default;
            try
            {
                _dealerSocket = CreateSocket();
                var message = new NetMQMessage();
                message.Append(command.ToString());
                message.Append(MessagePackSerializer.Serialize(values));
                while (_numberOfTriesLeft > 0)
                {
                    _numberOfTriesLeft--;
                    if (_numberOfTriesLeft == 0)
                    {
                        _logger.Here().Error("[Client - ERROR] Server seems to be offline, abandoning!");
                        break;
                    }

                    _dealerSocket.SendMultipartMessage(message);
                    if (_dealerSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(SocketTryReceiveFromMilliseconds),
                        out var msg, out _))
                    {
                        if (!string.IsNullOrEmpty(msg))
                        {
                            value = MessagePackSerializer.Deserialize<T>(msg.HexToByte());
                            _dealerSocket.Close();
                            break;
                        }
                    }
                    
                    _dealerSocket.Disconnect($"tcp://{_networkSettings.RemoteNode}");
                    _dealerSocket.Close();
                    _dealerSocket.Dispose();
                    _dealerSocket = CreateSocket();
                }
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex.Message);
            }
            finally
            {
                _numberOfTriesLeft = 4;
            }

            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private DealerSocket CreateSocket()
        {
            var clientPair = new NetMQCertificate();
            var dealerSocket = new DealerSocket();
            dealerSocket.Options.CurveServerKey = _networkSettings.RemoteNodePubKey.HexToByte().Skip(1).ToArray(); // X25519 32-byte key 
            dealerSocket.Options.CurveCertificate = clientPair;
            dealerSocket.Options.Identity = Util.RandomDealerIdentity();
            dealerSocket.Connect($"tcp://{_networkSettings.RemoteNode}");
            Thread.Sleep(100);
            return dealerSocket;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public void HasRemoteAddress()
        {
            var uriString = _networkSettings.RemoteNode;
            if (!string.IsNullOrEmpty(uriString)) return;
            _logger.Here().Error("Remote node address not set in config");
            throw new Exception("Address not specified");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            try
            {
                _dealerSocket.Disconnect($"tcp://{_networkSettings.RemoteNode}");
                _dealerSocket.Dispose();
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }
}

