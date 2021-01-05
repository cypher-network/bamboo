// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using BAMWallet.Rpc;
using System.IO;

namespace BAMWallet.Services
{
    public class SafeguardService : BackgroundService
    {
        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagService;
        private readonly IConfigurationSection _apiGatewaySection;
        private readonly Client _client;
        private readonly ILogger _logger;

        public SafeguardService(ISafeguardDownloadingFlagProvider safeguardDownloadingFlagService, IConfiguration configuration, ILogger<SafeguardService> logger)
        {
            _safeguardDownloadingFlagService = safeguardDownloadingFlagService;
            _apiGatewaySection = configuration.GetSection(RestCall.Gateway);
            _logger = logger;

            _client = new Client(configuration, _logger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                var needData = NeedNewSafeguardData();
                if (!needData)
                    return;

                _safeguardDownloadingFlagService.IsDownloading = true;

                var baseAddress = _client.GetBaseAddress();
                var path = _apiGatewaySection.GetSection(RestCall.Routing).GetValue<string>(RestCall.RestSafeguardTransactions.ToString());

                var blockArray = await _client.GetAsync<byte[]>(baseAddress, path, stoppingToken);
                if (blockArray != null)
                {
                    var fileStream = SafeguardData(DateTime.UtcNow.ToShortDateString());
                    await fileStream.WriteAsync(blockArray, stoppingToken);

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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool NeedNewSafeguardData()
        {
            var safeGuardPath = Path.Combine(Path.GetDirectoryName(Helper.Util.AppDomainDirectory()), "safeguard");
            var d = DateTime.UtcNow - TimeSpan.FromDays(1.8);

            if (Directory.Exists(safeGuardPath))
            {
                foreach (var filename in Directory.EnumerateFiles(safeGuardPath))
                {
                    string filenameWithoutPath = Path.GetFileNameWithoutExtension(filename);
                    if (filenameWithoutPath.Equals(d.ToShortDateString()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static Stream SafeguardData(string date)
        {
            var safeGuardPath = Path.Combine(Path.GetDirectoryName(Helper.Util.AppDomainDirectory()), Path.Combine("safeguard", $"{date}.json"));

            if (!Directory.Exists(safeGuardPath))
            {
                try
                {
                    Directory.CreateDirectory(safeGuardPath);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }

            var file = File.Open(safeGuardPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            return file;
        }
    }
}
