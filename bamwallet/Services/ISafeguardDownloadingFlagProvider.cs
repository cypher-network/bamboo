﻿// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.Services
{

    public class SafeguardDownloadingFlagProvider : ISafeguardDownloadingFlagProvider
    {
        private bool _isDownloading;
        public bool Downloading { get => _isDownloading; set => _isDownloading = value; }
    }
}