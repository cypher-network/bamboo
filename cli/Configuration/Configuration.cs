using System;
using System.IO;
using Cli.UI;

namespace Cli.Configuration
{
    public class Configuration
    {
        private IUserInterface _userInterface;

        public Configuration(IUserInterface userInterface)
        {
            _userInterface = userInterface;

            var networkConfiguration = new Network(userInterface);
            if (!networkConfiguration.Do())
            {
                Cancel();
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Environment       : " + networkConfiguration.Configuration.Environment);
            Console.WriteLine("Wallet API address: " + networkConfiguration.Configuration.WalletIPAddressPublic);
            Console.WriteLine("Wallet API port   : " + networkConfiguration.Configuration.WalletPort);
            Console.WriteLine("Node API address  : " + networkConfiguration.Configuration.NodeIPAddress);
            Console.WriteLine("Node API port     : " + networkConfiguration.Configuration.NodePort);
            Console.WriteLine();

            var configTemplate = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "Templates", Program.AppSettingsFile));
            var config = configTemplate
                .Replace("<ENVIRONMENT>", networkConfiguration.Configuration.Environment)
                .Replace("<WALLET_ENDPOINT_BIND>", $"http://{networkConfiguration.Configuration.WalletIPAddress}:{networkConfiguration.Configuration.WalletPort.ToString()}")
                .Replace("<WALLET_ENDPOINT_PUBLIC>", $"http://{networkConfiguration.Configuration.WalletIPAddressPublic}:{networkConfiguration.Configuration.WalletPort.ToString()}")
                .Replace("<NODE_ENDPOINT>", $"http://{networkConfiguration.Configuration.NodeIPAddress}:{networkConfiguration.Configuration.NodePort}");

            var configFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Program.AppSettingsFile);
            File.WriteAllText(configFileName, config);

            Console.WriteLine($"Configuration written to {configFileName}");
            Console.WriteLine();
        }

        private void Cancel()
        {
            var section = new UserInterfaceSection(
                "Cancel configuration",
                "Configuration cancelled",
                null);

            _userInterface.Do(section);
        }
    }
}