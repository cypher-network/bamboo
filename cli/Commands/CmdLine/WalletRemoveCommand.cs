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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BAMWallet.Extensions;
using McMaster.Extensions.CommandLineUtils;
using Constants = BAMWallet.HD.Constant;
using BAMWallet.HD;
using Cli.Commands.Common;
using Cli.Helper;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("remove", "Remove a wallet")]
    class WalletRemoveCommand : Command
    {
        private string _idToDelete;
        private readonly ILogger _logger;

        private static bool IsLoggedInWithWallet(SecureString identifier, Session activeSession)
        {
            return activeSession != null &&
                   string.Equals(identifier.FromSecureString(), activeSession.Identifier.FromSecureString());
        }

        private void DeleteWallet()
        {
            if (string.IsNullOrEmpty(_idToDelete))
            {
                _console.WriteLine("Wallet name cannot be empty.");
                return;
            }
            var baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            try
            {
                if (Directory.Exists(baseDir))
                {
                    var walletsDir = Path.Combine(baseDir, Constants.WALLET_DIR_SUFFIX);
                    if (Directory.Exists(walletsDir))
                    {
                        var files = Directory.GetFiles(walletsDir, Constants.WALLET_FILE_EXTENSION);
                        var deleted = false;
                        if (files.Any())
                        {
                            var walletFile = files.FirstOrDefault(x => string.Equals(Path.GetFileNameWithoutExtension(x), _idToDelete, StringComparison.CurrentCulture));
                            if (!string.IsNullOrEmpty(walletFile))
                            {
                                File.Delete(walletFile);
                                deleted = true;
                            }
                        }
                        if (!deleted)
                        {
                            _console.ForegroundColor = ConsoleColor.Red;
                            _console.WriteLine("Wallet with name: {0} does not exist. Command failed.", _idToDelete);
                            _console.ForegroundColor = ConsoleColor.White;
                        }
                        else
                        {
                            _console.ForegroundColor = ConsoleColor.Green;
                            _console.WriteLine("Wallet with name: {0} permanently deleted.", _idToDelete);
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
        public WalletRemoveCommand(IServiceProvider serviceProvider, ILogger logger)
            : base(typeof(WalletRemoveCommand), serviceProvider)
        {
            _logger = logger;
            Logout = false;
            _idToDelete = string.Empty;
        }

        public override Task Execute(Session activeSession = null)
        {
            var identifier = Prompt.GetString("Wallet Name:", null, ConsoleColor.Yellow);
            var isDeletionConfirmed = Prompt.GetYesNo(
                $"Are you sure you want to delete wallet with Identifier: {identifier}? (This action cannot be undone!)", false, ConsoleColor.Red);
            if (!isDeletionConfirmed) return Task.CompletedTask;
            _idToDelete = identifier;
            if (IsLoggedInWithWallet(identifier.ToSecureString(), activeSession))
            {
                Logout = true;
                DeleteWallet();
            }
            else
            {
                DeleteWallet();
            }

            return Task.CompletedTask;
        }

        public bool Logout { get; private set; }
    }
}