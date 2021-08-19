// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using Microsoft.Extensions.Hosting;
using BAMWallet.Rpc;
using BAMWallet.Helper;
using BAMWallet.Model;
using MessagePack;
using Microsoft.Extensions.Options;
using Serilog;

namespace BAMWallet.Services
{
    public class SafeguardService : BackgroundService
    {
        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagService;
        private readonly NetworkSettings _networkSettings;
        private readonly Client _client;
        private readonly ILogger _logger;

        public SafeguardService(ISafeguardDownloadingFlagProvider safeguardDownloadingFlagService, IOptions<NetworkSettings> networkSettings, ILogger logger)
        {
            _safeguardDownloadingFlagService = safeguardDownloadingFlagService;
            _networkSettings = networkSettings.Value;
            _logger = logger.ForContext("SourceContext", nameof(SafeguardService));

            _client = new Client(networkSettings.Value, _logger);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static Stream GetSafeguardData()
        {
            var safeGuardPath = SafeguardFilePath();
            var filePath = Directory.EnumerateFiles(safeGuardPath, "*.messagepack").Last();
            return File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static Transaction[] GetTransactions()
        {
            var byteArray = Util.StreamToArray(GetSafeguardData());
            var blocks = MessagePackSerializer.Deserialize<GenericList<Block>>(byteArray);
            blocks.Data.Shuffle();
            return blocks.Data.SelectMany(x => x.Txs).ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (NeedNewSafeguardData())
                {
                    _safeguardDownloadingFlagService.IsDownloading = true;

                    var baseAddress = _client.GetBaseAddress();
                    var blocks = _client.GetRangeAsync<Block>(baseAddress, _networkSettings.Routing.SafeguardTransactions, stoppingToken);
                    if (blocks != null)
                    {
                        var fileStream = SafeguardData(GetDays());
                        var buffer = MessagePackSerializer.Serialize(blocks, cancellationToken: stoppingToken);
                        fileStream.Write(buffer, 0, buffer.Count());
                        fileStream.Flush();
                        fileStream.Close();
                        _safeguardDownloadingFlagService.IsDownloading = false;
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, "SafeGuardService execution error");
            }
            finally
            {
                _safeguardDownloadingFlagService.IsDownloading = false;
            }
            return Task.FromResult<object>(null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static bool NeedNewSafeguardData()
        {
            var safeGuardPath = SafeguardFilePath();
            var d = GetDays();
            return !Directory.Exists(safeGuardPath) || Directory.EnumerateFiles(safeGuardPath)
                .Select(Path.GetFileNameWithoutExtension).All(filenameWithoutPath =>
                    !filenameWithoutPath.Equals(d.ToString("dd-MM-yyyy")));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static DateTime GetDays()
        {
            return DateTime.UtcNow - TimeSpan.FromDays(1.8);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static Stream SafeguardData(DateTime date)
        {
            var safeGuardPath = SafeguardFilePath();
            if (Directory.Exists(safeGuardPath))
                return File.Open($"{safeGuardPath}/{date:dd-MM-yyyy}.messagepack", FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.ReadWrite);
            try
            {
                Directory.CreateDirectory(safeGuardPath);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return File.Open($"{safeGuardPath}/{date:dd-MM-yyyy}.messagepack", FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static string SafeguardFilePath()
        {
            return Path.Combine(Path.GetDirectoryName(Util.AppDomainDirectory()), "safeguard");
        }
    }
}
