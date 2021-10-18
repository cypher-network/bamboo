// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BAMWallet.HD;
using Cli.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.AspNetCore.Hosting;

namespace Cli
{
    public static class Program
    {
        private const int MinimumConfigVersion = 1;

        public static async Task<int> Main(string[] args)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var appsettingsExists =
                File.Exists(Path.Combine(basePath, Constant.AppSettingsFile));

            if (args.FirstOrDefault(arg => arg == "--configure") != null)
            {
                if (appsettingsExists)
                {
                    // Do not return an error; this check is part of the application installation process
                    Console.WriteLine(
                        $"{Constant.AppSettingsFile} already exists. Please remove file before running configuration again");
                    return 0;
                }

                var ui = new TerminalUserInterface();
                var nc = new Configuration.Configuration(ui);
                return 0;
            }

            if (!appsettingsExists)
            {
                await Console.Error.WriteLineAsync($"{Constant.AppSettingsFile} not found. Please create one running 'clibamwallet --configure'");
                return 1;
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(Constant.AppSettingsFile, true)
                .AddCommandLine(args)
                .Build();

            var configVersion = config.GetSection(Constant.ConfigSectionNameConfigVersion);
            if (!int.TryParse(configVersion.Value, out var configVersionNumber) ||
                configVersionNumber < Constant.MinimumConfigVersion)
            {
                await Console.Error.WriteLineAsync($"Configuration file outdated. Please delete appsettings.json and create a new one running 'clibamwallet --configure'");
                return 1;
            }

            if (config.GetSection(Constant.ConfigSectionNameLog) != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config, Constant.ConfigSectionNameLog)
                    .CreateLogger();
            }
            else
            {
                throw new Exception(string.Format($"No \"{Constant.ConfigSectionNameLog}\" section found in appsettings.json", Constant.ConfigSectionNameLog));
            }

            try
            {
                Log.Information("Starting wallet");
                Log.Information($"Version: {BAMWallet.Helper.Util.GetAssemblyVersion()}");
                var builder = CreateWebHostBuilder(args, config);
                builder.UseConsoleLifetime();

                using var host = builder.Build();
                await host.RunAsync();
                await host.WaitForShutdownAsync();
            }
            catch (TaskCanceledException)
            {

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

            return 0;
        }

        private static IHostBuilder CreateWebHostBuilder(string[] args, IConfiguration configuration) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var walletEndpoint = configuration["NetworkSettings:WalletEndpoint"];
                    webBuilder
                        .UseStartup<Startup>()
                        .UseUrls(walletEndpoint)
                        .UseSerilog();
                });
    }
}
