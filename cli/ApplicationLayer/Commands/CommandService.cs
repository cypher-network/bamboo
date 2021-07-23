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
using System.Reflection;
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
        private static readonly ICommand[] fAllCommands = {

        };
        private static readonly ICommand[] fLoggedOutCommands = {
            //Wallet.WalletCreateCommand
        };
        private readonly IConsole console;
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        readonly IDictionary<string, ICommand> commands;
        private bool prompt = true;

        private Thread _t;

        public CommandService(IConsole cnsl, IServiceProvider provider, ILogger<CommandService> lgr)
        {
            console = cnsl;
            logger = lgr;
            serviceProvider = provider;

            commands = new Dictionary<string, ICommand>();

            console.CancelKeyPress += Console_CancelKeyPress;

            RegisterCommand(new Login(provider));
            RegisterCommand(new Logout(provider));
            RegisterCommand(new ExitCommand(provider));
            RegisterCommand(new WalletAddressCommand(provider));
            RegisterCommand(new WalletBalanceCommand(provider));
            RegisterCommand(new WalletCreateCommand(provider));
            RegisterCommand(new WalletListCommand(provider));
            RegisterCommand(new WalletReceivePaymentCommand(provider));
            RegisterCommand(new WalletReceivePaymentCommand(provider));
            RegisterCommand(new WalletRestoreCommand(provider));
            RegisterCommand(new WalletTransferCommand(provider));
            RegisterCommand(new WalletTxHistoryCommand(provider));
            RegisterCommand(new WalletVersionCommand(provider));
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
            commands.Add(command.Name, command);
        }

        private ICommand GetCommand(string arg)
        {
            if (commands.ContainsKey(arg))
            {
                return commands[arg];
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
            console.WriteLine();
            console.WriteLine("  Commands");

            foreach (var cmd in commands)
            {
                console.WriteLine($"    {cmd.Value.Name}".PadRight(25) + $"{cmd.Value.Description}");
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

            while (prompt)
            {
                string arg = Prompt.GetString("bamboo$", promptColor: ConsoleColor.Cyan);

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
                    Logger.LogException(console, logger, e);
                }
            }

            await ExitCleanly();
        }

        private async Task ExitCleanly()
        {
            prompt = false;

            console.WriteLine("Exiting...");

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
                var serviceInstance = serviceProvider.GetService(hostedProvider) as IHostedService;

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
                var serviceInstance = serviceProvider.GetService(hostedProvider) as IHostedService;

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
