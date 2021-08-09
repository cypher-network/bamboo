// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using System.Security;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

using McMaster.Extensions.CommandLineUtils;

using BAMWallet.Extensions;
using Constants = BAMWallet.HD.Constant;
using CLi.ApplicationLayer.Events;
using CLi.Helper;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("remove", "Removes a wallet and logs out if that wallet was used to login.")]
    class WalletRemoveCommand : Command
    {
        private object _lock;
        private bool _isSyncInProgress;
        private bool _isLogoutRequested;
        private string _idToDelete;
        private readonly ILogger _logger;

        private bool IsLoggedInWithWallet(SecureString identifier)
        {
            if (ActiveSession != null)
            {
                return String.Equals(identifier.ToUnSecureString(), ActiveSession.Identifier.ToUnSecureString());
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
            _isSyncInProgress = false;
            _isLogoutRequested = false;
            _idToDelete = String.Empty;
            _lock = new object();
            SyncCommand.OnSyncStateChanged += (o, e) =>
            {
                lock (_lock)
                {
                    _isSyncInProgress = (e.SyncStatus == SyncStateChanged.SyncState.SyncInProgress);
                    if (!_isSyncInProgress && _isLogoutRequested)
                    {
                        Logout();
                        DeleteWallet();
                        _isLogoutRequested = false;
                    }
                }
            };
        }

        public override Task Execute()
        {
            lock (_lock)
            {
                var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
                var isDeletionConfirmed = Prompt.GetYesNo(string.Format("Are you sure you want to delete wallet with Identifier: {0}? (This action cannot be undone!)", identifier.ToUnSecureString()), false, ConsoleColor.Red);

                if (isDeletionConfirmed)
                {
                    _idToDelete = identifier.ToUnSecureString();
                    if (IsLoggedInWithWallet(identifier))
                    {
                        _isLogoutRequested = true;
                        if (!_isSyncInProgress)
                        {
                            Logout();
                            DeleteWallet();
                        }
                    }
                    else
                    {
                        DeleteWallet();
                    }
                }
                return Task.CompletedTask;
            }
        }
    }
}