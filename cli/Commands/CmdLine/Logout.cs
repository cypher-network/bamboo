// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;

using CLi.ApplicationLayer.Events;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("logout", "Logs out and locks wallet.")]
    class Logout : Command
    {
        private object _lock;
        private bool _isSyncInProgress;
        private bool _isLogoutRequested;
        public Logout(IServiceProvider serviceProvider)
            : base(typeof(Logout), serviceProvider)
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

        public override void Execute()
        {
            lock (_lock)
            {
                _isLogoutRequested = true;
                if (!_isSyncInProgress)
                {
                    Logout();
                }
            }
        }
    }
}