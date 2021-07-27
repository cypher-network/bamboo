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
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CLi.Helper;
using CLi.ApplicationLayer.Commands.Wallet;
using CLi.ApplicationLayer.Commands.Vault;

namespace CLi.ApplicationLayer.Commands
{
    public class CommandService : HostedService, ICommandService
    {
        enum State {
            LoggedIn,
            Loggedout
        };
        private readonly IConsole _console;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        readonly IDictionary<string, ICommand> _commands;
        private bool _hasExited;
        private Thread _t;
        private State _commandServiceState;

        public CommandService(IConsole cnsl, IServiceProvider provider, ILogger<CommandService> lgr)
        {
            _serviceProvider = provider;
            _console = cnsl;
            _logger = lgr;
            _commands = new Dictionary<string, ICommand>();
            _console.CancelKeyPress += Console_CancelKeyPress;
            _commandServiceState = State.Loggedout;
            _hasExited = false;
            RegisterLoggedOutCommands();
            Command.LoginStateChanged += (o, e) =>
            {
                if(e.LoginStateChangedFrom == Events.LogInStateChanged.LoginEvent.Loggedout)
                {
                    _commandServiceState = State.LoggedIn;
                    RegisterLoggedInCommands();
                }
                else
                {
                    _commandServiceState = State.Loggedout;
                    RegisterLoggedOutCommands();
                }
            };
        }

        private void RegisterLoggedOutCommands()
        {
            _commands.Clear();
            RegisterCommand(new Login(_serviceProvider));
            RegisterCommand(new WalletCreateCommand(_serviceProvider));
            RegisterCommand(new WalletCreateMnemonicCommand(_serviceProvider));
            RegisterCommand(new WalletListCommand(_serviceProvider));
            RegisterCommand(new WalletRestoreCommand(_serviceProvider));
            RegisterCommand(new WalletVersionCommand(_serviceProvider));
            RegisterCommand(new ExitCommand(this));
        }

        private void RegisterLoggedInCommands()
        {
            _commands.Clear();
            RegisterCommand(new Logout(_serviceProvider));
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
            RegisterCommand(new ExitCommand(this));
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            ExitCleanly().GetAwaiter().GetResult();
        }

        public async Task Exit()
        {
            await ExitCleanly();
        }

        public void RegisterCommand(ICommand command)
        {
            _commands.Add(command.Name, command);
        }

        private ICommand GetCommand(string arg)
        {
            if (_commands.ContainsKey(arg))
            {
                return _commands[arg];
            }
            return null;
        }

        public async Task Execute(string arg)
        {
            var command = GetCommand(arg);

            if (command == null)
            {
                PrintHelp();
                return;
            }

            await command.Execute();
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

            Thread.Sleep(1500); //St.An. what's that?

            ClearCurrentConsoleLine();

            while (!_hasExited)
            {
                string arg = Prompt.GetString("bamboo$", promptColor: ConsoleColor.Cyan).Trim();

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }
                try
                {
                    await Execute(arg).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.LogException(_console, _logger, e);
                }
            }

            await ExitCleanly();
        }

        private async Task ExitCleanly()
        {
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
                var serviceInstance = _serviceProvider.GetService(hostedProvider) as IHostedService;

                if (serviceInstance != null)
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
            await Task.Run(() =>
            {
                _t = new Thread(async () => { await InteractiveCliLoop(); });
                _t.Start();
            });
        }
    }
}
