// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Model;
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

        [Obsolete]
        public static async Task<int> Main(string[] args)
        {
            try
            {
                //args = new string[] { "--configure" };
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var appsettingsExists =
                    File.Exists(Path.Combine(basePath, Constant.AppSettingsFile));

                if (args.FirstOrDefault(arg => arg == "--configure") != null)
                {
                    if (appsettingsExists)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        // Do not return an error; this check is part of the application installation process
                        Console.WriteLine($"{Constant.AppSettingsFile} already exists. Delete the file to continue (y) Or cancel (n)");
                        Console.ResetColor();
                        var line = Console.ReadLine();
                        if (line == "y")
                        {
                            File.Delete(Path.Combine(basePath, Constant.AppSettingsFile));
                        }
                        else
                        {
                            return 0;
                        }
                    }

                    var ui = new TerminalUserInterface();
                    var nc = new Configuration.Configuration(ui);

                    var storedAppSettings = BAMWallet.Helper.Util.WalletPath("appsettings");
                    try
                    {
                        if (File.Exists(storedAppSettings))
                        {
                            File.Delete(storedAppSettings);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write($"Something went wrong!" +
                                      $"\nBamboo might not have permissions to delete the file appsettings.db" +
                                      $"\nPlease manually delete the file : {storedAppSettings}\n" +
                                      $"Or run Bamboo with elevated permissions.");
                        Log.Error("{@Message}", ex.Message);
                    }

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
                    await Console.Error.WriteLineAsync("Configuration file outdated. Please delete appsettings.json and create a new one running 'clibamwallet --configure'");
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
                    throw new Exception($"No \"{Constant.ConfigSectionNameLog}\" section found in appsettings.json");
                }


                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(@$"
.______        ___      .___  ___. .______     ______     ______   
|   _  \      /   \     |   \/   | |   _  \   /  __  \   /  __  \  
|  |_)  |    /  ^  \    |  \  /  | |  |_)  | |  |  |  | |  |  |  | 
|   _  <    /  /_\  \   |  |\/|  | |   _  <  |  |  |  | |  |  |  | 
|  |_)  |  /  _____  \  |  |  |  | |  |_)  | |  `--'  | |  `--'  | 
|______/  /__/     \__\ |__|  |__| |______/   \______/   \______/  v{BAMWallet.Helper.Util.GetAssemblyVersion()}");
                Console.WriteLine("");
                Console.ResetColor();
                var builder = CreateWebHostBuilder(args, config);
                builder.UseConsoleLifetime();

                using var host = builder.Build();
                await host.RunAsync();
                await host.WaitForShutdownAsync();
            }
            catch (ObjectDisposedException)
            {
                // Ignore
                return 1;
            }
            catch (TaskCanceledException ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
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

        [Obsolete]
        private static IHostBuilder CreateWebHostBuilder(string[] args, IConfiguration configuration) => Host
            .CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
            {
                var fileStream = new FileStream(".LOCK", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                var walletEndpoint = configuration["NetworkSettings:WalletEndpoint"];
                var env = configuration["NetworkSettings:Environment"];
                var node = configuration["NetworkSettings:RemoteNode"];
                var nodePort = configuration["NetworkSettings:RemotePort"];
                var nodePk = configuration["NetworkSettings:RemoteNodePubKey"];
                var confirmations = configuration["NetworkSettings:NumberOfConfirmations"];
                var networkSettings = new NetworkSettings
                {
                    NumberOfConfirmations = Convert.ToUInt64(confirmations),
                    Environment = env,
                    RemoteNode = node,
                    RemotePort = Convert.ToInt32(nodePort),
                    RemoteNodePubKey = nodePk,
                    WalletEndpoint = walletEndpoint
                };

                Helper.Utils.SetConsoleTitle(networkSettings.Environment);

                var liteDatabase = BAMWallet.Helper.Util.LiteRepositoryAppSettingsFactory();
                if (!liteDatabase.Database.CollectionExists($"{nameof(NetworkSettings)}"))
                {
                    liteDatabase.Insert(networkSettings);
                }
                else
                {
                    networkSettings = BAMWallet.Helper.Util.LiteRepositoryAppSettingsFactory().Query<NetworkSettings>().First();
                }

                var endPoint = Helper.Utils.TryParseAddress(networkSettings.WalletEndpoint);
                var port = Helper.Utils.IsFreePort(endPoint.Port);
                try
                {
                    if (port == 0)
                    {
                        using var sr = new StreamReader(fileStream);
                        var pid = Convert.ToInt32(sr.ReadLine());
                        foreach (var process in Process.GetProcesses())
                        {
                            if (process.Id != pid) continue;
                            process.Kill();
                        }
                    }
                    else
                    {
                        using var sw = new StreamWriter(fileStream);
                        sw.WriteLine(Environment.ProcessId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }

                webBuilder.UseStartup<Startup>().UseUrls(networkSettings.WalletEndpoint).UseSerilog();
            });
    }
}
