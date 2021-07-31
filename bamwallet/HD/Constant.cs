// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace BAMWallet.HD
{
    public static class Constant
    {
        public const string Mainnet = "mainnet";
        public const string Testnet = "testnet";

        public const int NanoTan = 1000_000_000;

        public const string AppSettingsFile = "appsettings.json";
        public const string AppSettingsFileDev = "appsettings.Development.json";

        public const string ConfigSectionNameLog = "Log";
        public const string ConfigSectionNameConfigVersion = "ConfigVersion";

        public const int MinimumConfigVersion = 1;
    }
}