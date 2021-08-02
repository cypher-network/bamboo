// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using CLi.ApplicationLayer.Events;
namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("logout", "Logs out and locks wallet.")]
    class Logout : Command
    {
        private object _lock;
        private bool _isSyncInProgress;
        private bool _isLogoutRequested;
        public Logout(IServiceProvider serviceProvider) : base(typeof(Logout).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(Logout).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description), serviceProvider.GetService<IConsole>())
        {
            _isSyncInProgress = false;
            _isLogoutRequested = false;
            _lock = new object();
            SyncCommand.OnSyncStateChanged += (o, e) =>
            {
                lock (_lock)
                {
                    _isSyncInProgress = (e.SyncStatus == SyncStateChanged.SyncState.SyncInProgress);
                    if (!_isSyncInProgress && _isLogoutRequested)
                    {
                        Logout();
                        _isLogoutRequested = false;
                    }
                }
            };
        }

        public override Task Execute()
        {
            lock (_lock)
            {
                _isLogoutRequested = true;
                if (!_isSyncInProgress)
                {
                    Logout();
                }
                return Task.CompletedTask;
            }
        }
    }
}