// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using BAMWallet.Extensions;
using BAMWallet.Helper;
using MessagePack;
using BAMWallet.Model;
using NetMQ;
using NetMQ.Sockets;
using Serilog;

namespace BAMWallet.Rpc
{
    public class Client
    {
        private const int SocketTryReceiveFromMilliseconds = 30000;
        
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
                using var client = new DealerSocket($">tcp://{_networkSettings.RemoteNode}");
                client.Options.Identity = Util.RandomDealerIdentity();
                var message = new NetMQMessage();
                message.Append(command.ToString());
                message.Append(MessagePackSerializer.Serialize(values));
                client.SendMultipartMessage(message);
                if (!client.TryReceiveFrameString(TimeSpan.FromMilliseconds(SocketTryReceiveFromMilliseconds), out var msg, out _)) return default;
                if (string.IsNullOrEmpty(msg)) return default;
                value = MessagePackSerializer.Deserialize<T>(msg.HexToByte());
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex.Message);
            }

            return value;
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
    }
}

