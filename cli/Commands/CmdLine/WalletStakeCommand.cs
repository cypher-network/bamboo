// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;

namespace CLi.Commands.CmdLine;

[CommandDescriptor("stake", "Setup staking on your node")]
public class WalletStakeCommand: Command
{
    public WalletStakeCommand(IServiceProvider serviceProvider)
        : base(typeof(WalletStakeCommand), serviceProvider, true)
    {
    }

    public override async Task Execute(Session activeSession = null)
    {
        var seed = Prompt.GetPasswordAsSecureString("Seed:", ConsoleColor.Red);
        var rewardAddress = Prompt.GetString("Reward Address:", null, ConsoleColor.Yellow);
        var secret = Prompt.GetPasswordAsSecureString("Node Private Key:", ConsoleColor.Red);
        var token = Prompt.GetPasswordAsSecureString("Node Token:", ConsoleColor.Red);
        if (seed.Length != 0 && !string.IsNullOrEmpty(rewardAddress) && secret.Length != 0 &&
            token.Length != 0)
        {
            await Spinner.StartAsync("Setting up staking on your node ...", async spinner =>
            {
                try
                {
                    if (activeSession == null) return;
                    var stakeCredentialsRequest = new StakeCredentialsRequest
                    {
                        Passphrase = activeSession.Passphrase.ToArray(),
                        RewardAddress = rewardAddress.ToBytes(),
                        Seed = seed.ToArray()
                    };
                    var messageResponse = await _commandReceiver.SendStakeCredentials(activeSession,
                        stakeCredentialsRequest, secret.FromSecureString().HexToByte(),
                        token.FromSecureString().HexToByte());
                    if (!messageResponse.Value.Success)
                    {
                        spinner.Fail(messageResponse.Value.Message);
                    }
                    else
                    {
                        spinner.Succeed(messageResponse.Value.Message);
                    }
                }
                catch (Exception ex)
                {
                    _console.ForegroundColor = ConsoleColor.Red;
                    _console.WriteLine($"{ex.Message}");
                    _console.ForegroundColor = ConsoleColor.White;
                }
            }, Patterns.Hearts);
        }
    }
}