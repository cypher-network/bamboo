// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.Common
{
    public abstract class Command
    {
        protected readonly ICommandReceiver _walletService;
        protected readonly ICommandService _receiver;

        protected Command(Type commandType, IServiceProvider serviceProvider)
        {
             Name = commandType.GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name);
             Description = commandType.GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description);
            _walletService = serviceProvider.GetService<ICommandReceiver>();
            _receiver = serviceProvider.GetService<ICommandService>();
        }

        public abstract void Execute();

        public string Name { get; set; }
        public string Description { get; set; }
    }
}

// namespace Cli.Commands.Common
// {
//     public abstract class Command : Command
//     {
//         protected readonly TimingSettings _timingSettings;
//         protected readonly IConsole _console;
//         private static bool _isInitialized = false;
//         private static Timer _timeout;
//         private static LogInStateChanged.LoginEvent _loginState = LogInStateChanged.LoginEvent.Init;
//         private void OnTimeout(object source, ElapsedEventArgs e)
//         {
//             _console.ForegroundColor = ConsoleColor.Red;
//             _console.WriteLine("You have been logged out of the wallet due to inactivity. Please login again to use the wallet.");
//             _console.ForegroundColor = ConsoleColor.Cyan;
//             _console.Write("bamboo$ ");
//             _console.ForegroundColor = ConsoleColor.White;
//             Logout();
//             _console.ResetColor();
//         }

//         private void ReinitializeLogoutTimer()
//         {
//             if (_timeout != null)
//             {
//                 _timeout.Elapsed -= OnTimeout;
//                 _timeout.Stop();
//             }

//             _timeout = new Timer(TimeSpan.FromMinutes(_timingSettings.SessionTimeoutMins).TotalMilliseconds);
//             _timeout.Elapsed += OnTimeout;
//         }

//         protected Command(Type commandType, IServiceProvider serviceProvider)
//         {
//             Name = commandType.GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name);
//             Description = commandType.GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description);

//             _timingSettings = serviceProvider.GetService<IOptions<TimingSettings>>()?.Value ?? new();

//             _console = serviceProvider.GetService<IConsole>();
//             if (!_isInitialized)
//             {
//                 ActiveSession = null;
//                 _isInitialized = true;
//             }
//             ReinitializeLogoutTimer();
//         }
//         protected static Session ActiveSession { get; set; }
//         protected void Login()
//         {
//             ReinitializeLogoutTimer();
//             if (_loginState != LogInStateChanged.LoginEvent.LoggedIn)
//             {
//                 LoginStateChanged?.Invoke(this, new LogInStateChanged(LogInStateChanged.LoginEvent.LoggedIn, LogInStateChanged.LoginEvent.LoggedOut));
//                 _loginState = LogInStateChanged.LoginEvent.LoggedIn;
//             }
//             _timeout.Start();

//         }

//         public static void FreezeTimer()
//         {
//             _timeout.Stop();
//         }

//         public static void UnfreezeTimer()
//         {
//             _timeout.Start();
//         }

//         protected void Logout()
//         {
//             if (_loginState != LogInStateChanged.LoginEvent.LoggedOut)
//             {
//                 LoginStateChanged?.Invoke(this, new LogInStateChanged(LogInStateChanged.LoginEvent.LoggedOut, LogInStateChanged.LoginEvent.LoggedIn));
//                 _loginState = LogInStateChanged.LoginEvent.LoggedOut;
//             }
//             _timeout.Stop();
//             ActiveSession = null;
//         }

//         public string Name { get; set; }
//         public string Description { get; set; }
//         public abstract void Execute();
//         public static event EventHandler<LogInStateChanged> LoginStateChanged;
//     }
// }
