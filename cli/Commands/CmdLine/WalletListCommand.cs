// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using ConsoleTables;
using McMaster.Extensions.CommandLineUtils;
using BAMWallet.HD;
using Cli.Commands.Common;
namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("list", "Available wallets")]
    class WalletListCommand : Command
    {
        public WalletListCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletListCommand), serviceProvider, true)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            var request = _walletService.WalletList();

            if (request.Item1 is null)
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"Wallet list request failed: {request.Item2}!");
                _console.ForegroundColor = ConsoleColor.White;
            }

            var table = new ConsoleTable("Path");

            foreach (var key in request.Item1 as List<string>)
            {
                table.AddRow(key);
            }

            _console.WriteLine($"\n{table}");
        }
    }
}
