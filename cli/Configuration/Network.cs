// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cli.UI;

namespace Cli.Configuration
{
    public class Network
    {
        private readonly IUserInterface _userInterface;
        private readonly UserInterfaceChoice _optionCancel = new(string.Empty);
        private readonly IList<IPService> _ipServices = IPServices.Services;
        private readonly IPAddress _nodeTangramIPAddress = IPAddress.Parse("167.99.81.173");

        public class ConfigurationClass
        {
            public string Environment { get; set; }
            public IPAddress WalletIPAddress { get; set; }
            public IPAddress NodeIPAddress { get; set; }
            public ushort NodePort { get; set; } = 7946;
            public ushort WalletPort { get; set; } = 8001;
            public string NodePubKey { get; set; }
        }

        public ConfigurationClass Configuration { get; } = new();

        public Network(IUserInterface userInterface)
        {
            _userInterface = userInterface.SetTopic("Network");
        }

        public bool Do()
        {
            return StepIntroduction();
        }

        private bool SetPort(string prompt, out ushort port)
        {
            var section = new TextInput<ushort>(
                prompt,
                (string portString) => ushort.TryParse(portString, out _),
                (string portString) => ushort.Parse(portString));

            return _userInterface.Do(section, out port);
        }

        #region Introduction

        private bool StepIntroduction()
        {
            UserInterfaceChoice optionContinue = new("Continue network setup");

            var section = new UserInterfaceSection(
                "Network configuration",
                "The cypher wallet communicates with a node over an API interface. For a proper wallet/node " +
                "setup, the following components need to be configured:" + Environment.NewLine +
                Environment.NewLine +
                "- Your wallet can communicate with a local or remote node" + Environment.NewLine +
                "- The node can communicate with your wallet over your API port (TCP)" + Environment.NewLine,
                new[]
                {
                    optionContinue
                });

            var choice = _userInterface.Do(section);

            if (choice.Equals(optionContinue))
            {
                return StepEnvironment();
            }

            return false;
        }

        #endregion

        #region environment

        private bool StepEnvironment()
        {
            UserInterfaceChoice optionEnvironmentMainnet = new("mainnet");
            UserInterfaceChoice optionEnvironmentTestnet = new("testnet");

            var section = new UserInterfaceSection(
                "Environment",
                "The environment defines the network your wallet will be operating on.",
                new[]
                {
                    optionEnvironmentMainnet,
                    optionEnvironmentTestnet
                });

            var choiceEnvironment = _userInterface.Do(section);

            if (choiceEnvironment.Equals(optionEnvironmentMainnet))
            {
                Configuration.Environment = optionEnvironmentMainnet.Text;
                return StepNode();
            }
            
            if (choiceEnvironment.Equals(optionEnvironmentTestnet))
            {
                Configuration.Environment = optionEnvironmentTestnet.Text;
                return StepNode();
            }

            return false;
        }

        #endregion

        #region node
        private bool StepNode()
        {
            UserInterfaceChoice optionNodeTangram = new("Tangram Team-managed node (http://167.99.81.173:48655)");
            UserInterfaceChoice optionNodeCustom = new("Custom node");

            var section = new UserInterfaceSection(
                "Node",
                "You can connect to a public Tangram Team-managed node or a custom node like your own. You cannot stake on the Tangram Team-managed node.",
                new[]
                {
                    optionNodeTangram,
                    optionNodeCustom
                });

            var choiceNode = _userInterface.Do(section);

            if (choiceNode.Equals(optionNodeTangram))
            {
                Configuration.NodeIPAddress = _nodeTangramIPAddress;
                return StepWalletIPAddress();
            }

            if (choiceNode.Equals(optionNodeCustom))
            {
                return StepNodeCustom();
            }

            return false;
        }

        private bool StepNodeCustom()
        {
            UserInterfaceChoice optionYes = new("Yes");
            UserInterfaceChoice optionNo = new("No");

            var section = new UserInterfaceSection(
                "Remote node",
                "Is your node running on the same system as your wallet?",
                new[]
                {
                    optionYes,
                    optionNo
                });

            var choiceSameSystem = _userInterface.Do(section);

            if (choiceSameSystem.Equals(optionYes))
            {
                Configuration.WalletIPAddress = IPAddress.Loopback;
                Configuration.NodeIPAddress = IPAddress.Loopback;
                return StepNodePort();
            }

            if (choiceSameSystem.Equals(optionNo))
            {
                return StepNodeIPAddress();
            }

            return false;
        }

        private bool StepNodeIPAddress()
        {
            var section = new TextInput<IPAddress>(
                "Enter node IP address (e.g. 123.1.23.123)",
                ipAddress => IPAddress.TryParse(ipAddress, out _),
                ipAddress => IPAddress.Parse(ipAddress));

            var success = _userInterface.Do(section, out var ipAddress);
            if (success)
            {
                Configuration.NodeIPAddress = ipAddress;
                return StepNodePort();
            }

            return success;
        }

        private bool StepNodePort()
        {
            var section = new TextInput<ushort>(
                "Enter node API port (e.g. 7946)",
                (string portString) => ushort.TryParse(portString, out _),
                (string portString) => ushort.Parse(portString));

            var success = _userInterface.Do(section, out var port);
            if (success)
            {
                Configuration.NodePort = port;
                if (Configuration.NodeIPAddress.Equals(IPAddress.Loopback))
                {
                    return StepWalletPort();
                }

                return StepWalletIPAddress();
            }

            return success;
        }

        readonly UserInterfaceChoice _optionIpAddressManual = new("Manually enter IP address");
        readonly UserInterfaceChoice _optionIpAddressAuto = new("Find IP address automatically");

        private bool StepWalletIPAddress()
        {
            var section = new UserInterfaceSection(
                "Public IP address",
                "Your wallet needs to be able to communicate with the remote node. For this you need to " +
                "broadcast your public IP address, which is in many cases not the same as your local network " +
                "address. Addresses starting with 10.x.x.x, 172.16.x.x and 192.168.x.x are local addresses and " +
                "should not be broadcast to the network. When you do not know your public IP address, you can find " +
                "it by searching for 'what is my ip address'. This does not work if you configure a remote wallet, " +
                "like for example a VPS. You can also choose to find your public IP address automatically.",
                new[]
                {
                    _optionIpAddressManual,
                    _optionIpAddressAuto
                });

            var choiceIpAddress = _userInterface.Do(section);

            if (choiceIpAddress.Equals(_optionIpAddressManual))
            {
                return StepWalletIpAddressManual();
            }

            if (choiceIpAddress.Equals(_optionIpAddressAuto))
            {
                return StepWalletIpAddressAuto();
            }

            return false;
        }

        private bool StepWalletIpAddressManual()
        {
            var section = new TextInput<IPAddress>(
                "Enter IP address (e.g. 123.1.23.123)",
                ipAddress => IPAddress.TryParse(ipAddress, out _),
                ipAddress => IPAddress.Parse(ipAddress));

            var success = _userInterface.Do(section, out var ipAddress);
            if (success)
            {
                Configuration.WalletIPAddress = IPAddress.Parse("0.0.0.0");
                return StepWalletPort();
            }

            return success;
        }

        private bool StepWalletIpAddressAuto()
        {
            while (Configuration.WalletIPAddress == null)
            {
                var section = new UserInterfaceSection(
                    _optionIpAddressAuto.Text,
                    "Please choose the service to use for automatic IP address detection.",
                    _ipServices.ToList().Select(service =>
                        new UserInterfaceChoice(service.ToString())).ToArray());

                var choiceIpAddressService = _userInterface.Do(section);
                if (choiceIpAddressService.Equals(_optionCancel))
                {
                    return false;
                }

                try
                {
                    var selectedIpAddressService = _ipServices
                        .First(service => service.ToString() == choiceIpAddressService.Text);
                    Configuration.WalletIPAddress = IPAddress.Parse("0.0.0.0");
                }
                catch (Exception)
                {
                    // Cannot get IP address; ignore error
                }
            }

            return StepWalletPort();
        }

        private bool StepWalletPort()
        {
            var section = new TextInput<ushort>(
                "Enter wallet API port (e.g. 8001). The port must be different from the node's API port when running on the same system",
                (string portString) => ushort.TryParse(portString, out _),
                ushort.Parse);
            var success = _userInterface.Do(section, out var port);
            if (!success) return false;
            if (Configuration.NodeIPAddress.Equals(IPAddress.Loopback) && port == Configuration.NodePort)
            {
                return false;
            }

            Configuration.WalletPort = port;
            return StepWalletNodePubKey();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool StepWalletNodePubKey()
        {
            var section = new TextInput<string>(
                "Enter remote node public key from http://167.99.81.173:48655/member/peer",
                pubkey => !string.IsNullOrEmpty(pubkey), pubkey => pubkey);
            var success = _userInterface.Do(section, out var key);
            if (!success) return false;
            Configuration.NodePubKey = key;
            return true;
        }

        #endregion
    }
}