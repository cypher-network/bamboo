// Bamboo (c) by Tangram 
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.Logging;

namespace CLi.Helper
{
    public static class Logger
    {
        public static void LogException(IConsole console, ILogger logger, Exception e)
        {
            console.BackgroundColor = ConsoleColor.Red;
            console.ForegroundColor = ConsoleColor.White;
            console.WriteLine(e.ToString());
            logger.LogError(e, Environment.NewLine);
            console.ResetColor();
        }

        public static void LogWarning(IConsole console, ILogger logger, string message)
        {
            console.ForegroundColor = ConsoleColor.Yellow;
            console.WriteLine(message);
            console.ResetColor();
            logger.LogWarning(message);
        }
    }
}
