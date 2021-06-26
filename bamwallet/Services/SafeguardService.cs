﻿// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BAMWallet.Rpc;
using BAMWallet.Helper;
using BAMWallet.Model;
using MessagePack;
using Microsoft.Extensions.Options;

namespace BAMWallet.Services
{
    public class SafeguardService : BackgroundService
    {
        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagService;
        private readonly NetworkSettings _networkSettings;
        private readonly Client _client;
        private readonly ILogger _logger;

        public SafeguardService(ISafeguardDownloadingFlagProvider safeguardDownloadingFlagService, IOptions<NetworkSettings> networkSettings, ILogger<SafeguardService> logger)
        {
            _safeguardDownloadingFlagService = safeguardDownloadingFlagService;
            _networkSettings = networkSettings.Value;
            _logger = logger;

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
            return File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite); ;
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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                var needData = NeedNewSafeguardData();
                if (!needData) return;
                _safeguardDownloadingFlagService.IsDownloading = true;

                var baseAddress = _client.GetBaseAddress();
                if (baseAddress == null)
                {
                    throw new Exception("Cannot get base address");
                }

                var blocks = await _client.GetRangeAsync<Block>(baseAddress, _networkSettings.Routing.SafeguardTransactions, stoppingToken);
                if (blocks != null)
                {
                    var fileStream = SafeguardData(GetDays());
                    var buffer = MessagePackSerializer.Serialize(blocks, cancellationToken: stoppingToken);
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
