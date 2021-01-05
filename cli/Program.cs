// Bamboo (c) by Tangram 
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

using McMaster.Extensions.CommandLineUtils;

using CLi.ApplicationLayer.Commands;

using BAMWallet.HD;
using BAMWallet.Services;

namespace Cli
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File("Bamboo.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting host");

                await CreateHostBuilder(args);

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static async Task CreateHostBuilder(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.SetBasePath(Directory.GetCurrentDirectory());

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    services
                        .AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>()
                        .AddHostedService<SafeguardService>()
                        .AddSingleton<IWalletService, WalletService>()
                        .AddSingleton<ICommandService, CommandService>()
                        .AddSingleton<IHostedService, BAMWallet.Rpc.SelfHosted>()
                        .AddSingleton<IHostedService, CommandService>(sp =>
                        {
                            return sp.GetService<ICommandService>() as CommandService;
                        })
                        .AddLogging(config =>
                        {
                            config.ClearProviders();
                            config.AddProvider(new SerilogLoggerProvider(Log.Logger));
                        });

                    services.Add(new ServiceDescriptor(typeof(IConsole), PhysicalConsole.Singleton));
                })
                .UseSerilog((context, configuration) => configuration
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.File("Cli.log", rollingInterval: RollingInterval.Day))
                .UseConsoleLifetime();

            await builder.RunConsoleAsync();
        }
    }
}
