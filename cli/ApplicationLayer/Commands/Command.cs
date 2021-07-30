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
using McMaster.Extensions.CommandLineUtils;
namespace CLi.ApplicationLayer.Commands
{
    public abstract class Command : ICommand
    {
        protected readonly IConsole _console;
        private static bool _isInitialized = false;
        private static readonly double TIMEOUT = 1000 * 60 * 30;
        private static Timer _timeout = new Timer(TIMEOUT);
        private static LogInStateChanged.LoginEvent _loginState = LogInStateChanged.LoginEvent.Init;
        private void OnTimeout(object source, ElapsedEventArgs e)
        {
            _console.WriteLine("You have been logged out of the wallet due to inactivity. Please login again to use the wallet.");
            _console.ForegroundColor = ConsoleColor.Cyan;
            _console.Write("bamboo$ ");
            _console.ForegroundColor = ConsoleColor.White;
            Logout();
        }

        private void ReinitializeLogoutTimer()
        {
            _timeout.Elapsed -= OnTimeout;
            _timeout.Stop();
            _timeout = new Timer(TIMEOUT);
            _timeout.Elapsed += OnTimeout;
        }

        protected Command(string name, string description, IConsole console)
        {
            _console = console;
            Name = name;
            Description = description;
            if (!_isInitialized)
            {
                ActiveSession = null;
                _isInitialized = true;
            }
            ReinitializeLogoutTimer();
        }
        protected static Session ActiveSession { get; set; }
        protected void Login()
        {
            ReinitializeLogoutTimer();
            if (_loginState != LogInStateChanged.LoginEvent.LoggedIn)
            {
                LoginStateChanged?.Invoke(this, new LogInStateChanged(LogInStateChanged.LoginEvent.LoggedIn, LogInStateChanged.LoginEvent.LoggedOut));
                _loginState = LogInStateChanged.LoginEvent.LoggedIn;
            }
            _timeout.Start();

        }

        public static void FreezeTimer()
        {
            _timeout.Stop();
        }

        public static void UnfreezeTimer()
        {
            _timeout.Start();
        }

        protected void Logout()
        {
            if (_loginState != LogInStateChanged.LoginEvent.LoggedOut)
            {
                LoginStateChanged?.Invoke(this, new LogInStateChanged(LogInStateChanged.LoginEvent.LoggedOut, LogInStateChanged.LoginEvent.LoggedIn));
                _loginState = LogInStateChanged.LoginEvent.LoggedOut;
            }
            _timeout.Stop();
            ActiveSession = null;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public abstract Task Execute();
        public static event EventHandler<LogInStateChanged> LoginStateChanged;
    }
}
