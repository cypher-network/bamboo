﻿// Bamboo (c) by Tangram 
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using McMaster.Extensions.CommandLineUtils;

using ConsoleTables;

using BAMWallet.HD;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "address" }, "Find out your address")]
    class WalletAddressCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly IConsole _console;

        public WalletAddressCommand(IServiceProvider serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _console = serviceProvider.GetService<IConsole>();
        }

        public override Task Execute()
        {
            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);
            var session = _walletService.SessionAddOrUpdate(new Session(identifier, passphrase));

            var request = _walletService.Addresses(session.SessionId);
            if (!request.Success)
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("Address request failed.");
                _console.ForegroundColor = ConsoleColor.White;
                return Task.CompletedTask;
            }

            if (!request.Result.Any())
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("No address can be found.");
                _console.ForegroundColor = ConsoleColor.White;
                return Task.CompletedTask;
            }

            var table = new ConsoleTable("Address");

            foreach (var address in request.Result)
                table.AddRow(address);

            _console.WriteLine($"\n{table}");

            return Task.CompletedTask;
        }
    }
}
