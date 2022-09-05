// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using ConsoleTables;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;

namespace CLi.Commands.CmdLine;

[CommandDescriptor("stake", "Setup staking on your node")]
public class WalletStakeCommand : Command
{
    public WalletStakeCommand(IServiceProvider serviceProvider) : base(typeof(WalletStakeCommand), serviceProvider,
        true)
    {
    }

    public override async Task Execute(Session activeSession = null)
    {
        if (activeSession == null) return;
        _console.WriteLine(
            $"Last known staking amount: {_commandReceiver.GetLastKnownStakeAmount(activeSession).DivWithGYin()}");
        var continueYesNo = Prompt.GetYesNo("Continue setting up staking (y) or quit (n)", true, ConsoleColor.Yellow);
        if (!continueYesNo) return;
        BalanceProfile balanceProfile = null;
        await Spinner.StartAsync("Checking confirmed balance(s) ...", spinner =>
        {
            balanceProfile = _commandReceiver.GetBalanceProfile(activeSession);
            if (balanceProfile == null)
            {
                spinner.Fail("Nothing to see.");
                return Task.CompletedTask;
            }

            var table = new ConsoleTable("Payments", "Coinstake", "Coinbase", "Change", "Balance");
            table.AddRow($"{balanceProfile.Payment:F9}", $"{balanceProfile.Coinstake:F9}",
                $"{balanceProfile.Coinbase:F9}", $"{balanceProfile.Change:F9}", $"{balanceProfile.Balance:F9}");
            table.Configure(o => o.NumberAlignment = Alignment.Right);
            _console.WriteLine($"\n{table}");
            _console.WriteLine("\n");
            _console.WriteLine();
            _console.WriteLine();
            Thread.Sleep(100);
            return Task.CompletedTask;
        }, Patterns.Hearts);
        decimal stakeAmount = 0;
        var yesNoStakeAmount =
            Prompt.GetYesNo($"Use wallet max balance {balanceProfile.Balance} (y) or specify an amount (n)", true,
                ConsoleColor.Yellow);
        if (!yesNoStakeAmount)
        {
            var amount = Prompt.GetString("Stake Amount:", null, ConsoleColor.Red);
            if (decimal.TryParse(amount, out var t))
            {
                if (t <= 0)
                {
                    _console.ForegroundColor = ConsoleColor.Red;
                    _console.WriteLine("Amount is less than zero or is zero");
                    _console.ResetColor();
                    return;
                }
                stakeAmount = t;
            }
            else
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("Amount not in the correct format");
                _console.ResetColor();
                return;
            }
        }

        var rewardAddress = string.Empty;
        var request = _commandReceiver.Address(activeSession);
        if (request.Item1 is null)
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine($"Unable to get this wallet Address: {request.Item2}");
            _console.ResetColor();
        }
        else
        {
            rewardAddress = request.Item1 as string;
        }

        var yesNoAddress = true;
        if (!string.IsNullOrEmpty(rewardAddress))
        {
            _console.ForegroundColor = ConsoleColor.Gray;
            _console.WriteLine($"   This wallets address: {rewardAddress}");
            yesNoAddress =
                Prompt.GetYesNo($"Use this wallet address: {rewardAddress} (y) or specify the wallet address (n)", true,
                    ConsoleColor.Yellow);
        }
        else
        {
            rewardAddress = Prompt.GetString("Wallet Address:", null, ConsoleColor.Yellow);
        }

        if (!yesNoAddress && !string.IsNullOrEmpty(rewardAddress))
        {
            rewardAddress = Prompt.GetString("Wallet Address:", null, ConsoleColor.Yellow);
        }

        var seed = Prompt.GetPasswordAsSecureString("Wallet Seed:", ConsoleColor.Red);
        var secret = Prompt.GetPasswordAsSecureString("Node Private Key:", ConsoleColor.Red);
        var token = Prompt.GetPasswordAsSecureString("Node Token:", ConsoleColor.Red);
        if (seed.Length != 0 && secret.Length != 0 && token.Length != 0)
        {
            var stakingEnabled = false;
            await Spinner.StartAsync($"Checking staking is enabled ... {_commandReceiver.NetworkSettings().RemoteNode} ...",
                async spinner =>
                {
                    try
                    {
                        var stakeCredentialsRequest = new StakeCredentialsRequest
                        {
                            Seed = seed.ToArray()
                        };
                        var messageResponse = await _commandReceiver.StakeEnabledCredentials(stakeCredentialsRequest,
                            secret.FromSecureString().HexToByte(), token.FromSecureString().HexToByte());
                        if (!messageResponse.Value.Success)
                        {
                            spinner.Fail(messageResponse.Value.Message);
                            return Task.CompletedTask; ;
                        }

                        _console.WriteLine();

                        Thread.Sleep(100);

                        if (messageResponse.Value.Message == "Staking enabled")
                        {
                            stakingEnabled = true;
                            spinner.Succeed(messageResponse.Value.Message);
                        }
                        else
                        {
                            spinner.Info(messageResponse.Value.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _console.ForegroundColor = ConsoleColor.Red;
                        _console.WriteLine($"{ex.Message}");
                        _console.ResetColor();
                    }

                    return Task.CompletedTask; ;
                }, Patterns.Hearts);


            if (stakingEnabled)
            {
                var yesNoStakeEnabled =
                    Prompt.GetYesNo($"Staking is enabled. To exit staking setup (y) or continue (n)", true,
                        ConsoleColor.Yellow);
                if (yesNoStakeEnabled)
                {
                    return;
                }
            }

            Balance[] balances = null;
            await Spinner.StartAsync("Checking confirmed balance(s) ...", spinner =>
            {
                balances = _commandReceiver.GetBalances(in activeSession);
                return Task.CompletedTask;
            }, Patterns.Hearts);
            Output[] listOutputs;
            if (stakeAmount != 0)
            {
                var balances1 = balances;
                var spend = balances
                    .Where(balance =>
                        !balance.Commitment.IsLockedOrInvalid() && balance.Total >= stakeAmount.ConvertToUInt64() &&
                        balance.Total <= balances1.Max(m => m.Total)).OrderByDescending(x => x.Total)
                    .Select(x => x.Total).Aggregate((x, y) =>
                        x - stakeAmount.ConvertToUInt64() < y - stakeAmount.ConvertToUInt64() ? x : y);
                balances1 = balances.Where(b => b.Total >= stakeAmount.ConvertToUInt64() && b.Total <= spend).ToArray();
                listOutputs = balances1.Select(balance => new Output
                {
                    C = balance.Commitment.C,
                    E = balance.Commitment.E,
                    N = balance.Commitment.N,
                    T = balance.Commitment.T
                }).ToArray();
                balanceProfile = _commandReceiver.GetBalanceProfile(activeSession, balances1);
                var yesNoContinue =
                    Prompt.GetYesNo($"Closest stake amount: {balanceProfile.Balance} to continue (y) or cancel (n)",
                        false, ConsoleColor.Yellow);
                if (!yesNoContinue)
                {
                    _console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine("Staking has been cancelled");
                    _console.ResetColor();
                    return;
                }
            }
            else
            {
                listOutputs = balances.Select(balance => new Output
                {
                    C = balance.Commitment.C,
                    E = balance.Commitment.E,
                    N = balance.Commitment.N,
                    T = balance.Commitment.T
                }).ToArray();
                stakeAmount = balanceProfile.Balance;
            }

            await Spinner.StartAsync($"Setting up staking on node {_commandReceiver.NetworkSettings().RemoteNode} ...",
                async spinner =>
                {
                    try
                    {
                        var stakeCredentialsRequest = new StakeCredentialsRequest
                        {
                            Passphrase = activeSession.Passphrase.ToArray(),
                            RewardAddress = rewardAddress.ToBytes(),
                            Seed = seed.ToArray()
                        };
                        var messageResponse = await _commandReceiver.SendStakeCredentials(stakeCredentialsRequest,
                            secret.FromSecureString().HexToByte(), token.FromSecureString().HexToByte(), listOutputs);
                        if (!messageResponse.Value.Success)
                        {
                            spinner.Fail(messageResponse.Value.Message);
                            return;
                        }

                        _commandReceiver.SaveLastKnownStakeAmount(activeSession, stakeAmount.ConvertToUInt64());

                        _console.WriteLine();

                        Thread.Sleep(100);

                        spinner.Succeed(messageResponse.Value.Message);
                    }
                    catch (Exception ex)
                    {
                        _console.ForegroundColor = ConsoleColor.Red;
                        _console.WriteLine($"{ex.Message}");
                        _console.ResetColor();
                    }
                }, Patterns.Hearts);
        }
    }
}