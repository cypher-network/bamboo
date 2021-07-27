// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using System.Timers;
using CLi.ApplicationLayer.Events;
using BAMWallet.HD;
namespace CLi.ApplicationLayer.Commands
{
    public abstract class Command : ICommand
    {
        private static bool _isInitialized = false;
        private static readonly double TIMEOUT = 60000*30;
        private Timer _timeout = new Timer(TIMEOUT);
        private void OnTimeout(object source, ElapsedEventArgs e)
        {
            Logout();
        }

        protected Command(string name, string description)
        {
            Name = name;
            Description = description;
            ActiveSession = null;
            if(!_isInitialized)
            {
                _timeout.Elapsed += OnTimeout;
                _isInitialized = true;
            }
        }
        protected static Session ActiveSession { get; set;}
        protected void Login()
        {
            _timeout.Stop();
            LoginStateChanged?.Invoke(this, new LogInStateChanged(LogInStateChanged.LoginEvent.LoggedIn, LogInStateChanged.LoginEvent.LoggedOut));
            _timeout.Start();
            _timeout.Enabled = true;
        }
        protected void Logout()
        {
            LoginStateChanged?.Invoke(this, new LogInStateChanged(LogInStateChanged.LoginEvent.LoggedOut, LogInStateChanged.LoginEvent.LoggedIn));
            _timeout.Stop();
            _timeout.Enabled = false;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public abstract Task Execute();
        public static event EventHandler<LogInStateChanged> LoginStateChanged;
    }
}
