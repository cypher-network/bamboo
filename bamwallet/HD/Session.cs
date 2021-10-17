// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Security;
using BAMWallet.Extensions;
using BAMWallet.Helper;
using BAMWallet.Model;
using LiteDB;

namespace BAMWallet.HD
{
    public class Session
    {
        public SecureString Identifier { get; }
        public SecureString Passphrase { get; }
        public Guid SessionId { get; set; }
        public SessionType SessionType { get; set; }
        public LiteRepository Database { get; }
        public bool Syncing { get; set; }

        /// <summary>
        /// Multiple key sets not supported, thus we can simply return the only one keyset create during wallet creation.
        /// </summary>
        /// <returns>The one and only KeySet</returns>
        public KeySet KeySet => Database.Query<KeySet>().First();

        public bool IsValid => IsIdentifierValid(Identifier);

        public static bool AreCredentialsValid(SecureString identifier, SecureString passphrase)
        {
            return IsIdentifierValid(identifier) && IsPassPhraseValid(identifier, passphrase);
        }

        private static bool IsIdentifierValid(SecureString identifier)
        {
            return File.Exists(Util.WalletPath(identifier.ToUnSecureString()));
        }

        public Session(SecureString identifier, SecureString passphrase)
        {
            Identifier = identifier;
            Passphrase = passphrase;
            SessionId = Guid.NewGuid();
            if (!IsValid)
            {
                throw new FileLoadException($"Wallet with ID: {identifier.ToUnSecureString()} not found!");
            }
            Database = Util.LiteRepositoryFactory(identifier, passphrase);
        }

        private static bool IsPassPhraseValid(SecureString id, SecureString pass)
        {
            var connectionString = new ConnectionString
            {
                Filename = Util.WalletPath(id.ToUnSecureString()),
                Password = pass.ToUnSecureString(),
                Connection = ConnectionType.Shared
            };
            using var db = new LiteDatabase(connectionString);
            var collection = db.GetCollection<KeySet>();
            try
            {
                if (collection.Count() == 1)
                {
                    return true;
                }
            }
            catch (LiteException)
            {
                return false;
            }

            return false;
        }
    }
}