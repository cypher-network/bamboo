// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.HD
{
    public static class Constant
    {
        public const string Mainnet = "mainnet";
        public const string Testnet = "testnet";

        public const int Yin = 1;
        public const int KYin = 1000;
        public const int MYin = 1000_000;
        public const int GYin = 1000_000_000;
        public const long MicroAether = 1_000_000_000_000;
        public const long MilliAether = 1_000_000_000_000_000;
        public const long Aether = 1_000_000_000_000_000_000;

        public const string AppSettingsFile = "appsettings.json";
        public const string AppSettingsDbFile = "appsettings.db";
        public const string AppSettingsFileDev = "appsettings.Development.json";

        public const string ConfigSectionNameLog = "Log";
        public const string ConfigSectionNameConfigVersion = "ConfigVersion";

        public const int MinimumConfigVersion = 1;

        public const string WALLET_DIR_SUFFIX = "wallets";

        public const string WALLET_FILE_EXTENSION = "*.db";

        public const string HD_PATH = "m/44'/847177'/0'/0/";
    }
}