// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using Microsoft.Extensions.DependencyInjection;
using BAMWallet.HD;
using BAMWallet.Helper;
using ConsoleTables;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("address", "Find out your address")]
    class WalletAddressCommand : Command
    {
        private readonly ICommandReceiver _walletService;
        public WalletAddressCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletAddressCommand), serviceProvider)
        {
            _walletService = serviceProvider.GetService<ICommandReceiver>();
        }

        public override void Execute()
        {
            this.Login();
            using var KeepLoginState = new RAIIGuard(Command.FreezeTimer, Command.UnfreezeTimer);
            var session = ActiveSession;

            var request = _walletService.Address(session);
            if (request.Item1 is null)
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"Address request failed : {request.Item2}");
                _console.ForegroundColor = ConsoleColor.White;
            }

            var table = new ConsoleTable("Address");
            table.AddRow(request.Item1 as string);
            _console.WriteLine($"\n{table}");
        }
    }
}
