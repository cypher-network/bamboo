// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BAMWallet.Rpc;
using BAMWallet.Helper;
using BAMWallet.Model;
using MessagePack;

namespace BAMWallet.Services
{
    public class SafeguardService : BackgroundService
    {
        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagService;
        private readonly IConfigurationSection _networkSection;
        private readonly Client _client;
        private readonly ILogger _logger;

        public SafeguardService(ISafeguardDownloadingFlagProvider safeguardDownloadingFlagService, IConfiguration configuration, ILogger<SafeguardService> logger)
        {
            _safeguardDownloadingFlagService = safeguardDownloadingFlagService;
            _networkSection = configuration.GetSection(Constant.Network);
            _logger = logger;

            _client = new Client(configuration, _logger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Stream GetSafeguardData()
        {
            var safeGuardPath = SafeguardFilePath();
            var filePath = Directory.EnumerateFiles(safeGuardPath, "*.messagepack").Last();
            return File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite); ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Transaction[] GetTransactions()
        {
            var byteArray = Util.ReadFully(GetSafeguardData());
            var blocks = MessagePackSerializer.Deserialize<GenericList<Block>>(byteArray);
            blocks.Data.Shuffle();
            return blocks.Data.SelectMany(x => x.Txs).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                var needData = NeedNewSafeguardData();
                if (!needData) return;
                _safeguardDownloadingFlagService.IsDownloading = true;
                var baseAddress = _client.GetBaseAddress();
                var path = _networkSection.GetSection(Constant.Routing)
                    .GetValue<string>(RestCall.RestSafeguardTransactions);
                var blocks = await _client.GetRangeAsync<Block>(baseAddress, path, stoppingToken);
                if (blocks != null)
                {
                    var fileStream = SafeguardData(GetDays());
                    var buffer = MessagePackSerializer.Serialize(blocks);
                    await fileStream.WriteAsync(buffer, stoppingToken);
                    await fileStream.FlushAsync(stoppingToken);
                    fileStream.Close();
                    _safeguardDownloadingFlagService.IsDownloading = false;
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< SafeguardService.ExecuteAsync >>>: {ex}");
            }
            finally
            {
                _safeguardDownloadingFlagService.IsDownloading = false;
            }
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
