using System;
using System.IO;
using BAMWallet.HD;
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
            Console.WriteLine("Wallet API port   : " + networkConfiguration.Configuration.WalletPort);
            Console.WriteLine("Node API address  : " + networkConfiguration.Configuration.NodeIPAddress);
            Console.WriteLine("Node API port     : " + networkConfiguration.Configuration.NodePort);
            Console.WriteLine("Run silently     : " + networkConfiguration.Configuration.RunSilently);
            Console.WriteLine();

            var configTemplate = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "Templates", Constant.AppSettingsFile));
            var config = configTemplate
                .Replace("<ENVIRONMENT>", networkConfiguration.Configuration.Environment)
                .Replace("<WALLET_ENDPOINT_BIND>",
                    $"http://{networkConfiguration.Configuration.WalletIPAddress}:{networkConfiguration.Configuration.WalletPort.ToString()}")
                .Replace("<NODE_ENDPOINT>",
                    $"http://{networkConfiguration.Configuration.NodeIPAddress}:{networkConfiguration.Configuration.NodePort}")
                .Replace("<WALLET_RUN_SILENTLY>", $"http://{networkConfiguration.Configuration.RunSilently}");

            var configFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constant.AppSettingsFile);
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