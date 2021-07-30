using System;

namespace CLi.ApplicationLayer.Events
{
    public class LogInStateChanged : EventArgs
    {
        public enum LoginEvent
        {
            LoggedIn,
            LoggedOut,
            Init
        };
        public LoginEvent LoginStateChangedTo { get; }
        public LoginEvent LoginStateChangedFrom { get; }

        public LogInStateChanged(LoginEvent to, LoginEvent from)
        {
            LoginStateChangedTo = to;
            LoginStateChangedFrom = from;
        }
    };
}