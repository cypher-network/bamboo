using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Helper;
using McMaster.Extensions.CommandLineUtils;

namespace CLi.ApplicationLayer.Commands
{
    public static class Shared
    {
        public static void CreateWalletFromKnownMnemonic(IConsole console, IWalletService walletService)
        {
            using var mnemonic = Prompt.GetPasswordAsSecureString("Mnemonic:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var id = walletService.CreateWallet(mnemonic, passphrase);
            var path = Util.WalletPath(id);

            console.WriteLine($"Wallet ID: {id}");
            console.WriteLine($"Wallet Path: {path}");
        }
    }
}
