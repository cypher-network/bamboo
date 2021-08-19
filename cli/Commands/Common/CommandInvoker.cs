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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BAMWallet.HD;
using BAMWallet.Model;
using BAMWallet.Helper;
using Cli.Commands.CmdLine;
using CLi.Helper;
using FuzzySharp;
using McMaster.Extensions.CommandLineUtils;
namespace Cli.Commands.Common
{
    public class CommandInvoker : HostedService, ICommandService
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
        private readonly SyncCommand _syncCommand;
        private bool _hasExited;
        private State _loginState = State.LoggedOut;
        protected readonly TimingSettings _timingSettings;
        private System.Timers.Timer _timeout = null;
        Session _activeSession = null;

        private void FreezeTimer()
        {
            _timeout.Stop();
        }
        private void UnfreezeTimer()
        {
            _timeout.Start();
        }
        private void OnTimeout(object source, System.Timers.ElapsedEventArgs e)
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine("You have been logged out of the wallet due to inactivity. Please login again to use the wallet.");
            _console.ForegroundColor = ConsoleColor.Cyan;
            _console.Write("bamboo$ ");
            _console.ForegroundColor = ConsoleColor.White;
            Logout();
            _console.ResetColor();
        }

        private void ReinitializeLogoutTimer()
        {
            if (_timeout != null)
            {
                _timeout.Elapsed -= OnTimeout;
                _timeout.Stop();
            }

            _timeout = new System.Timers.Timer(TimeSpan.FromMinutes(_timingSettings.SessionTimeoutMins).TotalMilliseconds);
            _timeout.Elapsed += OnTimeout;
            _timeout.Start();
        }

        private void OnLogout()
        {
            if (_loginState != State.LoggedOut)
            {
                _loginState = State.LoggedOut;
                RegisterLoggedOutCommands();
            }
            _timeout.Stop();
            _activeSession = null;
        }
        private void OnLogin()
        {
            if (_loginState != State.LoggedIn)
            {
                _loginState = State.LoggedIn;
                RegisterLoggedInCommands();
            }
            if(_loginState == State.LoggedIn)
            {
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
            RegisterLoggedOutCommands();
            _syncCommand = new SyncCommand(walletService, provider);
        }

        private void RegisterLoggedOutCommands()
        {
            _commands.Clear();
            RegisterCommand(new Login(_serviceProvider));
            RegisterCommand(new WalletRemoveCommand(_serviceProvider, _logger));
            RegisterCommand(new WalletCreateCommand(_serviceProvider));
            RegisterCommand(new WalletCreateMnemonicCommand(_serviceProvider));
            RegisterCommand(new WalletListCommand(_serviceProvider));
            RegisterCommand(new WalletRestoreCommand(_serviceProvider));
            RegisterCommand(new WalletVersionCommand(_serviceProvider));
            RegisterCommand(new ExitCommand(this, _serviceProvider));
        }

        private void RegisterLoggedInCommands()
        {
            _commands.Clear();
            RegisterCommand(new Logout(_serviceProvider));
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
            RegisterCommand(new ExitCommand(this, _serviceProvider));
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            ExitCleanly().GetAwaiter().GetResult();
        }

        public void RegisterCommand(Command command)
        {
            _commands.Add(command.Name, command);
        }

        public void Execute(string arg)
        {
            _commandQueue.Add(_commands[arg]);
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

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public async Task InteractiveCliLoop()
        {
            await StartAllHostedProviders();

            ClearCurrentConsoleLine();

            while (!_hasExited)
            {
                var args = Prompt.GetString("bamboo$", promptColor: ConsoleColor.Cyan)?.TrimEnd()?.Split(' ');

                if ((args == null) || (args.Length == 1 && string.IsNullOrEmpty(args[0])) || (args.Length > 1))
                {
                    continue;
                }
                if (args[0] == "--help" || args[0] == "?" || args[0] == "/?" || args[0] == "help")
                {
                    PrintHelp();
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
                    continue;
                }
                try
                {
                    Execute(args[0]);
                }
                catch (Exception e)
                {
                    Logger.LogException(_console, _logger, e);
                }
            }
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
                        if(cmd.RefreshLogin)
                        {
                            OnLogin();
                        }
                        using var freezeTimer = new RAIIGuard(FreezeTimer, UnfreezeTimer);
                        cmd.Execute(_activeSession);
                        if(cmd is LoginCommand)
                        {
                            _activeSession = (cmd as LoginCommand).ActiveSession;
                        }
                        if(cmd is LogoutCommand)
                        {
                            OnLogout();
                        }
                        if(cmd is ExitCommand)
                        {
                            _commandQueue.CompleteAdding();
                        }
                    }
                }
            });

            await ExitCleanly();
        }

        private async Task ExitCleanly()
        {
            _commandQueue.CompleteAdding();
            _hasExited = true;

            _console.WriteLine("Exiting...");

            await StopAllHostedProviders();

            Environment.Exit(0);
        }

        private IEnumerable<Type> FindAllHostedServiceTypes()
        {
            //  Concrete Service Types

            var type = typeof(HostedService);
            var concreteServiceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p != GetType() && p != typeof(HostedService));

            //  Find interfaces that implement IHostedService
            foreach (var concreteServiceType in concreteServiceTypes)
            {
                var interfaces = concreteServiceType.GetInterfaces();

                foreach (var inf in interfaces)
                {
                    if (inf == typeof(IHostedService))
                    {
                        continue;
                    }

                    var implements = inf.GetInterfaces().Any(x => x == typeof(IHostedService));

                    if (implements)
                    {
                        yield return inf;
                    }
                }
            }
        }

        private async Task StartAllHostedProviders()
        {
            var hostedProviders = FindAllHostedServiceTypes();

            foreach (var hostedProvider in hostedProviders)
            {
                if (_serviceProvider.GetService(hostedProvider) is IHostedService serviceInstance)
                {
                    await serviceInstance.StartAsync(new CancellationToken());
                }
            }
        }

        private async Task StopAllHostedProviders()
        {
            var hostedProviders = FindAllHostedServiceTypes();

            foreach (var hostedProvider in hostedProviders)
            {
                var serviceInstance = _serviceProvider.GetService(hostedProvider) as IHostedService;

                if (serviceInstance != null)
                {
                    await serviceInstance.StopAsync(new CancellationToken());
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                await InteractiveCliLoop();
            });
        }
    }
}
