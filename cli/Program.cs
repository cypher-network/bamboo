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
using Cli.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.AspNetCore.Hosting;

namespace Cli
{
    public static class Program
    {
        public const string AppSettingsFile = "appsettings.json";
        private const string AppSettingsFileDev = "appsettings.Development.json";

        public static async Task<int> Main(string[] args)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var appsettingsExists = File.Exists(Path.Combine(basePath, AppSettingsFile));

            if (args.FirstOrDefault(arg => arg == "--configure") != null)
            {
                if (appsettingsExists)
                {
                    // Do not return an error; this check is part of the application installation process
                    Console.WriteLine(
                        $"{AppSettingsFile} already exists. Please remove file before running configuration again");
                    return 0;
                }

                var ui = new TerminalUserInterface();
                var nc = new Configuration.Configuration(ui);
                return 0;
            }

            if (!appsettingsExists)
            {
                await Console.Error.WriteLineAsync($"{AppSettingsFile} not found. Please create one running 'clibamwallet --configure'");
                return 1;
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(AppSettingsFile, false)
                .AddJsonFile(AppSettingsFileDev, true)
                .AddCommandLine(args)
                .Build();

            const string logSectionName = "Log";
            if (config.GetSection(logSectionName) != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config, logSectionName)
                    .CreateLogger();
            }
            else
            {
                throw new Exception(string.Format($"No \"{@logSectionName}\" section found in appsettings.json", logSectionName));
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
                    var listening = configuration["NetworkSettings:Listening"];
                    webBuilder
                        .UseStartup<Startup>()
                        .UseUrls(listening)
                        .UseSerilog();
                });
    }
}
