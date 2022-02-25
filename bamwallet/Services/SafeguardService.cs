// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
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
using System.Globalization;

namespace BAMWallet.Services
{
    public class SafeguardService : BackgroundService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagService;
        private readonly Client _client;
        private readonly ILogger _logger;

        public SafeguardService(IHostApplicationLifetime lifeTime, ISafeguardDownloadingFlagProvider safeguardDownloadingFlagService, ILogger logger)
        {
            _applicationLifetime = lifeTime;
            _safeguardDownloadingFlagService = safeguardDownloadingFlagService;
            _logger = logger.ForContext("SourceContext", nameof(SafeguardService));
            _client = new Client(_logger);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static Stream GetSafeguardData()
        {
            var safeGuardPath = SafeguardFilePath();
            var actualSafeGuardFile = Directory.EnumerateFiles(safeGuardPath, "*.messagepack").OrderBy(x =>
                DateTime.ParseExact(Path.GetFileNameWithoutExtension(x), "dd-MM-yyyy", CultureInfo.InvariantCulture)
            ).Last();
            return new FileStream(actualSafeGuardFile, FileMode.Open, FileAccess.Read);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static Transaction[] GetTransactions()
        {
            var byteArray = Util.StreamToArray(GetSafeguardData());
            var blocks = MessagePackSerializer.Deserialize<List<Block>>(byteArray);
            blocks.Shuffle();
            return blocks.SelectMany(x => x.Txs).ToArray();
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
                DeleteSafeguardData();
                if (NeedNewSafeguardData())
                {
                    _safeguardDownloadingFlagService.TryDownloading = true;
                    _client.HasRemoteAddress();
                    var safeguardBlocksResponse =
                        _client.Send<SafeguardBlocksResponse>(new Parameter
                        { MessageCommand = MessageCommand.GetSafeguardBlocks });
                    if (safeguardBlocksResponse != null)
                    {
                        var fileStream = SafeguardData(GetDays());
                        var buffer = MessagePackSerializer.Serialize(safeguardBlocksResponse.Blocks,
                            cancellationToken: stoppingToken);
                        fileStream.Write(buffer, 0, buffer.Length);
                        fileStream.Flush();
                        fileStream.Close();
                        _safeguardDownloadingFlagService.TryDownloading = false;
                    }
                    else
                    {
                        throw new Exception("SafeGuard timed out. Connection to the node cannot be established");
                    }
                }
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                _logger.Here().Fatal(ex, "SafeGuardService execution error");
            }
            finally
            {
                _safeguardDownloadingFlagService.TryDownloading = false;
            }

            return Task.CompletedTask;
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
        private static void DeleteSafeguardData()
        {
            var safeGuardPath = SafeguardFilePath();
            try
            {
                if (Directory.Exists(safeGuardPath))
                {
                    Directory.Delete(safeGuardPath, true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
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
