// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using BAMWallet.Extensions;
using LiteDB;
using Newtonsoft.Json.Linq;
using BAMWallet.Helper;
using BAMWallet.Model;

namespace BAMWallet.HD
{
    public class Session : IEqualityComparer<Session>
    {
        public SecureString Identifier { get; }
        public JObject LastError { get; set; }
        public SecureString Passphrase { get; }
        public Guid SessionId { get; set; }
        public SessionType SessionType { get; set; }
        public WalletTransaction WalletTransaction { get; set; }
        public LiteRepository Database { get; set; }
        public bool Syncing { get; set; }

        public bool IsValid
        {
            get
            {
                return File.Exists(Util.WalletPath(Identifier.ToUnSecureString()));
            }
        }

        public Session(SecureString identifier, SecureString passphrase)
        {
            Identifier = identifier;
            Passphrase = passphrase;
            SessionId = Guid.NewGuid();
            if (!IsValid)
            {
                throw new FileLoadException(string.Format("Wallet with ID: {0} and Pass: {1} not found!", identifier, passphrase));
            }
            Database = Util.LiteRepositoryFactory(identifier, passphrase);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(Session x, Session y)
        {
            return x.Identifier == y.Identifier && x.Passphrase == y.Passphrase && x.SessionId == y.SessionId;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public int GetHashCode(Session session)
        {
            Session s = session;
            return s.SessionId.GetHashCode();
        }
    }
}