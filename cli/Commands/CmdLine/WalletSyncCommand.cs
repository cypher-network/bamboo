// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;
using Cli.Commands.Common;
using Kurukuru;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CLi.Commands.CmdLine
{
    [CommandDescriptor("sync", "syncs wallet with chain")]
    public class WalletSyncCommand : Command
    {
        private readonly ILogger _logger;
        private Spinner _spinner;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public WalletSyncCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletSyncCommand), serviceProvider, true)
        {
            _logger = serviceProvider.GetService<ILogger<WalletSyncCommand>>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activeSession"></param>
        public override void Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                Spinner.StartAsync("Syncing wallet ...", spinner =>
                {
                    _spinner = spinner;
                    try
                    {
                        _commandReceiver.SyncWallet(activeSession);
                        spinner.Succeed();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                        throw;
                    }
                    return Task.CompletedTask;
                }, Patterns.Arc);
            }
        }
    }
}