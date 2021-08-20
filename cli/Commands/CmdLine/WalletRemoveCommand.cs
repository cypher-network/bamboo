// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.IO;
using System.Linq;
using System.Security;
using Microsoft.Extensions.Logging;
using BAMWallet.Extensions;
using McMaster.Extensions.CommandLineUtils;
using Constants = BAMWallet.HD.Constant;
using BAMWallet.HD;
using Cli.Commands.Common;
using Cli.Helper;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("remove", "Removes a wallet and logs out if that wallet was used to login.")]
    class WalletRemoveCommand : Command
    {
        private bool _isLogoutRequested;
        private string _idToDelete;
        private readonly ILogger _logger;

        private bool IsLoggedInWithWallet(SecureString identifier, Session activeSession)
        {
            if (activeSession != null)
            {
                return String.Equals(identifier.ToUnSecureString(), activeSession.Identifier.ToUnSecureString());
            }
            else
            {
                return false;
            }
        }

        private void DeleteWallet()
        {
            if (_idToDelete != String.Empty)
            {
                var baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                try
                {
                    if (Directory.Exists(baseDir))
                    {
                        var walletsDir = Path.Combine(baseDir, Constants.WALLET_DIR_SUFFIX);
                        if (Directory.Exists(walletsDir))
                        {
                            var files = Directory.GetFiles(walletsDir, Constants.WALLET_FILE_EXTENSION);
                            bool deleted = false;
                            if (files.Any())
                            {
                                var walletFile = files.Where(x => String.Equals(Path.GetFileNameWithoutExtension(x), _idToDelete, StringComparison.CurrentCulture)).FirstOrDefault();
                                if (!String.IsNullOrEmpty(walletFile))
                                {
                                    File.Delete(walletFile);
                                    deleted = true;
                                }
                            }
                            if (!deleted)
                            {
                                _console.ForegroundColor = ConsoleColor.Red;
                                _console.WriteLine("Wallet with id: {0} does not exist. Command failed.", _idToDelete);
                                _console.ForegroundColor = ConsoleColor.White;
                            }
                            else
                            {
                                _console.ForegroundColor = ConsoleColor.Green;
                                _console.WriteLine("Wallet with id: {0} permenantly deleted.", _idToDelete);
                                _console.ForegroundColor = ConsoleColor.White;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(_console, _logger, ex);
                }
                _idToDelete = String.Empty;
            }
        }
        public WalletRemoveCommand(IServiceProvider serviceProvider, ILogger logger)
            : base(typeof(WalletRemoveCommand), serviceProvider)
        {
            _logger = logger;
            _isLogoutRequested = false;
            _idToDelete = String.Empty;
        }

        public override void Execute(Session activeSession = null)
        {
            var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            var isDeletionConfirmed = Prompt.GetYesNo(string.Format("Are you sure you want to delete wallet with Identifier: {0}? (This action cannot be undone!)", identifier.ToUnSecureString()), false, ConsoleColor.Red);
            if (isDeletionConfirmed)
            {
                _idToDelete = identifier.ToUnSecureString();
                if (IsLoggedInWithWallet(identifier, activeSession))
                {
                    Logout = true;
                    DeleteWallet();
                }
                else
                {
                    DeleteWallet();
                }
            }
        }

        public bool Logout
        {
            get
            {
                return _isLogoutRequested;
            }
            private set
            {
                _isLogoutRequested = value;
            }
        }
    }
}