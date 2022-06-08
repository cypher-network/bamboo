using System;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;

namespace CLi.Commands.CmdLine;

[CommandDescriptor("settings", "Manage app settings")]
public class WalletAppSettingsCommand : Command
{
    public WalletAppSettingsCommand(IServiceProvider serviceProvider)
        : base(typeof(WalletAppSettingsCommand), serviceProvider)
    {
    }

    public override Task Execute(Session activeSession = null)
    {
        var networkSettings = BAMWallet.Helper.Util.LiteRepositoryAppSettingsFactory().Query<NetworkSettings>().First();
        if (networkSettings == null)
        {
            _console.WriteLine("Unable to find network settings.");
            return Task.CompletedTask;
        }

        _console.ForegroundColor = ConsoleColor.Yellow;
        _console.WriteLine("");
        _console.WriteLine($"[1] Environment: {networkSettings.Environment}");
        _console.WriteLine($"[2] Wallet endpoint [http://0.0.0.0:8001]: {networkSettings.WalletEndpoint}");
        _console.WriteLine($"[3] Node [127.0.0.1:7946]: {networkSettings.RemoteNode}");
        _console.WriteLine($"[4] Node public key: {networkSettings.RemoteNodePubKey}");
        _console.WriteLine($"[5] Number of confirmations: {networkSettings.NumberOfConfirmations}");
        _console.WriteLine("");
        _console.ResetColor();

        var env = Prompt.GetString("Environment:", null, ConsoleColor.Green);
        var walletEndpoint = Prompt.GetString("Wallet endpoint:", null, ConsoleColor.Green);
        var node = Prompt.GetString("Node:", null, ConsoleColor.Green);
        var nodePk = Prompt.GetString("Node public key:", null, ConsoleColor.Green);
        var nrConfirmations = Prompt.GetInt("Number of confirmations:", 1, ConsoleColor.Green);

        if (!string.IsNullOrEmpty(env))
        {
            if (env.Equals(Constant.Mainnet, StringComparison.Ordinal) || env.Equals(Constant.Testnet, StringComparison.Ordinal))
            {
                networkSettings.Environment = env;
            }
            else
            {
                networkSettings.Environment = Constant.Testnet;
            }
        }

        if (!string.IsNullOrEmpty(walletEndpoint))
        {
            if (networkSettings.WalletEndpoint != walletEndpoint)
            {
                networkSettings.WalletEndpoint = walletEndpoint;
            }
        }
        
        if (!string.IsNullOrEmpty(node))
        {
            if (networkSettings.RemoteNode != node)
            {
                networkSettings.RemoteNode = node;
            }
        }

        if (!string.IsNullOrEmpty(nodePk))
        {
            if (networkSettings.RemoteNodePubKey != nodePk)
            {
                networkSettings.RemoteNodePubKey = nodePk;
            }
        }

        if (networkSettings.NumberOfConfirmations != (ulong)nrConfirmations)
        {
            networkSettings.NumberOfConfirmations = (ulong)nrConfirmations;
        }

        var liteDatabase = BAMWallet.Helper.Util.LiteRepositoryAppSettingsFactory();
        if (!liteDatabase.Database.CollectionExists("networksettings"))
        {
            liteDatabase.Insert(networkSettings);
            _console.WriteLine("Network settings saved.");
        }
        else
        {
            liteDatabase.Update(networkSettings);
            _console.WriteLine("Network settings updated.");
        }

        _commandReceiver.SetNetworkSettings();
        
        return Task.CompletedTask;
    }
}