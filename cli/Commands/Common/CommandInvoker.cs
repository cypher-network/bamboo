// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BAMWallet.HD;
using BAMWallet.Helper;
using BAMWallet.Model;
using Cli.Commands.CmdLine;
using CLi.Commands.CmdLine;
using Cli.Helper;
using FuzzySharp;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.Common
{
    public class CommandInvoker : ICommandService
    {
        enum State
        {
            LoggedIn,
            LoggedOut
        };
        private readonly IConsole _console;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, Command> _commands;
        private readonly BlockingCollection<Command> _commandQueue = new BlockingCollection<Command>();
        private bool _hasExited;
        private State _loginState = State.LoggedOut;
        protected readonly TimingSettings _timingSettings;
        private PausableTimer _timeout = null;
        private PausableTimer _syncTimer = null;
        Session _activeSession = null;
        AutoResetEvent _cmdFinishedEvent = new AutoResetEvent(true);
        CancellationTokenSource _cmdProcessorCancellationSource = new CancellationTokenSource();

        private void OnSyncInternal(object source, System.Timers.ElapsedEventArgs e)
        {
            if (_loginState == State.LoggedIn)
            {
                _commandQueue.Add(new SyncCommand(_serviceProvider));
            }
        }

        private void FreezeTimers()
        {
            if (_loginState == State.LoggedIn)
            {
                _timeout.Pause();
                _syncTimer.Pause();
            }
        }
        private void UnfreezeTimers()
        {
            if (_loginState == State.LoggedIn)
            {
                _timeout.Resume();
                _syncTimer.Resume();
            }
        }

        private void OnTimeout(object source, System.Timers.ElapsedEventArgs e)
        {
            _commandQueue.Add(new LogoutCommand(_serviceProvider, true));
        }

        private void ReinitializeSyncTimer()
        {
            if (_syncTimer != null)
            {
                _syncTimer.Stop();
                _syncTimer.Elapsed -= OnSyncInternal;
            }
            _syncTimer = new PausableTimer(TimeSpan.FromSeconds(_timingSettings.SyncIntervalSecs).TotalMilliseconds, true);
            _syncTimer.Elapsed += OnSyncInternal;
            _syncTimer.Start();
        }

        private void ReinitializeLogoutTimer()
        {
            if (_timeout == null)
            {
                _timeout = new PausableTimer(TimeSpan.FromMinutes(_timingSettings.SessionTimeoutMins).TotalMilliseconds);
                _timeout.Elapsed += OnTimeout;
                _timeout.Start();
            }
            else
            {
                _timeout.Restart();
            }
        }

        private void OnLogout()
        {
            if (_loginState != State.LoggedOut)
            {
                _loginState = State.LoggedOut;
                RegisterLoggedOutCommands();
            }
            _timeout.Stop();
            _syncTimer.Stop();
            _activeSession = null;
        }

        private void RefreshTimeout()
        {
            if (_loginState == State.LoggedIn)
            {
                _timeout.Restart();
            }
        }

        private void OnLogin()
        {
            if (_loginState != State.LoggedIn)
            {
                _loginState = State.LoggedIn;
                RegisterLoggedInCommands();
                ReinitializeSyncTimer();
                ReinitializeLogoutTimer();
            }
        }

        public CommandInvoker(IConsole cnsl, IServiceProvider provider, ILogger<CommandInvoker> lgr, ICommandReceiver walletService)
        {
            _serviceProvider = provider;
            _console = cnsl;
            _logger = lgr;
            _commands = new Dictionary<string, Command>();
            _console.CancelKeyPress += Console_CancelKeyPress;
            _hasExited = false;
            _timingSettings = provider.GetService<IOptions<TimingSettings>>()?.Value ?? new();
        }

        private void RegisterLoggedOutCommands()
        {
            _commands.Clear();
            RegisterCommand(new LoginCommand(_serviceProvider));
            RegisterCommand(new WalletRemoveCommand(_serviceProvider, _logger));
            RegisterCommand(new WalletCreateCommand(_serviceProvider));
            RegisterCommand(new WalletCreateMnemonicCommand(_serviceProvider));
            RegisterCommand(new WalletListCommand(_serviceProvider));
            RegisterCommand(new WalletRestoreCommand(_serviceProvider));
            RegisterCommand(new WalletVersionCommand(_serviceProvider));
            RegisterCommand(new ExitCommand(_serviceProvider));
            RegisterCommand(new WalletAppSettingsCommand(_serviceProvider));
        }

        private void RegisterLoggedInCommands()
        {
            _commands.Clear();
            RegisterCommand(new LogoutCommand(_serviceProvider));
            RegisterCommand(new WalletSyncCommand(_serviceProvider));
            RegisterCommand(new WalletStakeCommand(_serviceProvider));
            RegisterCommand(new WalletRemoveCommand(_serviceProvider, _logger));
            RegisterCommand(new WalletCreateCommand(_serviceProvider));
            RegisterCommand(new WalletCreateMnemonicCommand(_serviceProvider));
            RegisterCommand(new WalletListCommand(_serviceProvider));
            RegisterCommand(new WalletRestoreCommand(_serviceProvider));
            RegisterCommand(new WalletVersionCommand(_serviceProvider));
            RegisterCommand(new WalletAddressCommand(_serviceProvider));
            RegisterCommand(new WalletBalanceCommand(_serviceProvider));
            RegisterCommand(new WalletReceivePaymentCommand(_serviceProvider));
            RegisterCommand(new WalletRecoverTransactionsCommand(_serviceProvider));
            RegisterCommand(new WalletTransferCommand(_serviceProvider));
            RegisterCommand(new WalletTxHistoryCommand(_serviceProvider));
            RegisterCommand(new WalletAddressBookCommand(_serviceProvider));
            RegisterCommand(new ExitCommand(_serviceProvider));
            //RegisterCommand(new WalletAppSettingsCommand(_serviceProvider));
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            StopCommandProcessing();
        }

        public void RegisterCommand(Command command)
        {
            _commands.Add(command.Name, command);
        }

        public void EnqueueCommand(Command command)
        {
            _commandQueue.Add(command);
        }

        public void Execute(string arg)
        {
            _commandQueue.Add(_commands[arg]);
            if (arg.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            {
                StopCommandProcessing();
            }
        }

        private void PrintHelp()
        {
            _console.WriteLine();
            _console.WriteLine("  Commands");

            foreach (var cmd in _commands)
            {
                _console.WriteLine($"    {cmd.Value.Name}".PadRight(25) + $"{cmd.Value.Description}");
            }
        }

        private async Task ProcessCmdLineInput()
        {
            try
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(100);
                    while (!_hasExited)
                    {
                        _cmdFinishedEvent.WaitOne();

                        Thread.Sleep(25);

                        var args = _loginState == State.LoggedIn
                            ? Prompt.GetString($"{_activeSession.Identifier.FromSecureString()}$",
                                promptColor: ConsoleColor.Cyan)?.TrimEnd()?.Split(' ')
                            : Prompt.GetString("bamboo$", promptColor: ConsoleColor.Cyan)?.TrimEnd()?.Split(' ');

                        if ((args == null) || (args.Length == 1 && string.IsNullOrEmpty(args[0])) || (args.Length > 1))
                        {
                            _cmdFinishedEvent.Set();
                            continue;
                        }
                        if (args[0] == "--help" || args[0] == "?" || args[0] == "/?" || args[0] == "help")
                        {
                            PrintHelp();
                            _cmdFinishedEvent.Set();
                            continue;
                        }
                        if (!_commands.ContainsKey(args[0]))
                        {
                            var bestMatch = Process.ExtractOne(args[0], _commands.Keys, cutoff: 60);
                            if (null != bestMatch)
                            {
                                _console.WriteLine("Command: {0} not found. Did you mean {1}?", args[0], bestMatch.Value);
                            }
                            else
                            {
                                _console.WriteLine("Command: {0} not found. Here is the list of available commands:", args[0]);
                                PrintHelp();
                            }
                            _cmdFinishedEvent.Set();
                        }
                        else
                        {
                            try
                            {
                                Execute(args[0]);
                            }
                            catch (Exception e)
                            {
                                Logger.LogException(_console, _logger, e);
                            }
                        }
                    }
                }, _cmdProcessorCancellationSource.Token);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void StopCommandProcessing()
        {
            _hasExited = true;
            _commandQueue.CompleteAdding();
            _cmdProcessorCancellationSource.Cancel();
        }

        private async Task ProcessCommands()
        {
            await Task.Run(() =>
            {
                while (!_commandQueue.IsCompleted)
                {
                    Command cmd = null;
                    try
                    {
                        cmd = _commandQueue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        //thrown when queue is completed
                    }

                    if (cmd != null)
                    {
                        if (cmd.RefreshLogin)
                        {
                            RefreshTimeout();
                        }
                        using var freezeTimers = new RAIIGuard(FreezeTimers, UnfreezeTimers);
                        try
                        {
                            cmd.Execute(_activeSession);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(_console, _logger, ex);
                        }
                        if (!(cmd is SyncCommand || (cmd is LogoutCommand && (cmd as LogoutCommand).AutomaticLogout == true)))
                        {
                            _cmdFinishedEvent.Set();
                        }
                        if (cmd is LoginCommand)
                        {
                            _activeSession = (cmd as LoginCommand).ActiveSession;
                            if (_activeSession != null)
                            {
                                OnLogin();
                            }
                        }
                        if ((cmd is LogoutCommand) || ((cmd is WalletRemoveCommand) && (cmd as WalletRemoveCommand).Logout))
                        {
                            _activeSession = null;
                            OnLogout();
                        }
                        if (cmd is ExitCommand)
                        {
                            StopCommandProcessing();
                        }
                    }
                }
            });
        }

        public async Task InteractiveCliLoop()
        {
            RegisterLoggedOutCommands();
            Task inputProcessor = ProcessCmdLineInput();
            Task cmdProcessor = ProcessCommands();
            await Task.WhenAll(inputProcessor, cmdProcessor);
            ExitCleanly();
        }

        private void ExitCleanly()
        {
            _console.WriteLine("Exiting...");
            Environment.Exit(0);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () => { await InteractiveCliLoop(); }, cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.Run(StopCommandProcessing, cancellationToken);
        }
    }
}
