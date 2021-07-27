// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using CLi.ApplicationLayer.Events;
using System;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    abstract class LoginBase : Command
    {
        public static event EventHandler<LogInStateChanged> LoginStateChanged;

        public LoginBase(string name, string description) : base(name, description)
        {
        }

        protected void Login()
        {
            LoginStateChanged?.Invoke(this, new LogInStateChanged(LogInStateChanged.LoginEvent.LoggedIn, LogInStateChanged.LoginEvent.Loggedout));
        }

        protected void Logout()
        {
            LoginStateChanged?.Invoke(this, new LogInStateChanged(LogInStateChanged.LoginEvent.Loggedout, LogInStateChanged.LoginEvent.LoggedIn));
        }
    }
}