using System;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;

namespace CLi.Commands.CmdLine;

[CommandDescriptor("settings", "Mange app settings")]
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
        _console.WriteLine($"[2] Wallet endpoint: {networkSettings.WalletEndpoint}");
        _console.WriteLine($"[3] Node: {networkSettings.RemoteNode}");
        _console.WriteLine($"[4] Node public key: {networkSettings.RemoteNodePubKey}");
        _console.WriteLine($"[5] Number of confirmations: {networkSettings.NumberOfConfirmations}");
        _console.WriteLine("");
        _console.ResetColor();

        var env = Prompt.GetString("Environment:", Constant.Testnet, ConsoleColor.Green);
        if (env != Constant.Testnet)
        {
            if (!string.Equals(env, Constant.Mainnet, StringComparison.Ordinal))
            {
                env = Constant.Mainnet;
            }
        }

        var walletEndpoint = Prompt.GetString("Wallet endpoint:", null, ConsoleColor.Green);
        var node = Prompt.GetString("Node:", null, ConsoleColor.Green);
        var nodePk = Prompt.GetString("Node public key:", null, ConsoleColor.Green);
        var nrConfirmations = Prompt.GetInt("Number of confirmations:", 1, ConsoleColor.Green);

        if (!string.IsNullOrEmpty(env))
        {
            if (networkSettings.Environment != env)
            {
                networkSettings.Environment = env;
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

        return Task.CompletedTask;
    }
}