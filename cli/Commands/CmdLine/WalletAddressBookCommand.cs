// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using ConsoleTables;
using McMaster.Extensions.CommandLineUtils;

namespace CLi.Commands.CmdLine;

[CommandDescriptor("book", "Manage an recipient on your address book")]
public class WalletAddressBookCommand : Command
{
    public WalletAddressBookCommand(IServiceProvider serviceProvider)
        : base(typeof(WalletAddressBookCommand), serviceProvider, false)
    {
    }

    public override Task Execute(Session activeSession = null)
    {
        if (activeSession == null) return Task.CompletedTask;

        _console.ForegroundColor = ConsoleColor.Green;
        _console.WriteLine("Please enter in an option:");
        _console.ForegroundColor = ConsoleColor.Gray;
        _console.ForegroundColor = ConsoleColor.Yellow;
        _console.WriteLine("Add       [1]\n" + "Update    [2]\n" + "Remove    [3]\n" + "List      [4]");
        _console.ForegroundColor = ConsoleColor.White;
        var crud = Prompt.GetInt("Option:", 4, ConsoleColor.Magenta);

        switch (crud)
        {
            case 1:
                Add(activeSession);
                break;
            case 2:
                Update(activeSession);
                break;
            case 3:
                Remove(activeSession);
                break;
            case 4:
                List(activeSession);
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activeSession"></param>
    private void List(Session activeSession)
    {
        var result = _commandReceiver.ListAddressBook(activeSession);
        if (result.Item1 is { })
        {
            var table = new ConsoleTable("Created", "Name", "Address", "Description");
            if (result.Item1 is AddressBook[] addressBooks)
                foreach (var addressBook in addressBooks)
                {
                    table.AddRow(addressBook.Created.ToString("dd-MM-yyyy HH:mm:ss"), addressBook.Name,
                        addressBook.RecipientAddress, addressBook.Description);
                }

            table.Configure(o => o.NumberAlignment = Alignment.Right);
            _console.WriteLine($"\n{table}");
            _console.WriteLine("\n");
        }
        else
        {
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine(result.Item2);
            _console.ForegroundColor = ConsoleColor.White;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activeSession"></param>
    private void Remove(Session activeSession)
    {
        var address = Prompt.GetString("Address/Name:", null, ConsoleColor.Green);
        var addressBook = new AddressBook { Name = address };
        var result = _commandReceiver.FindAddressBook(activeSession, ref addressBook);
        if (result.Item1 != null)
        {
            addressBook = result.Item1 as AddressBook;
            var table = new ConsoleTable("Created", "Name", "Address", "Description");
            table.AddRow(addressBook.Created.ToString("dd-MM-yyyy HH:mm:ss"), addressBook.Name,
                addressBook.RecipientAddress, addressBook.Description);
            table.Configure(o => o.NumberAlignment = Alignment.Right);
            _console.WriteLine($"\n{table}");
            _console.WriteLine("\n");
            var yesno = Prompt.GetYesNo($"Remove {address}?", false);
            if (!yesno) return;
            var removed = _commandReceiver.RemoveAddressBook(activeSession, ref addressBook);
            _console.WriteLine(removed.Item2);
        }
        else
        {
            _console.WriteLine(result.Item2);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activeSession"></param>
    private void Add(Session activeSession)
    {
        var name = Prompt.GetString("Name:", null, ConsoleColor.Green);
        var address = Prompt.GetString("Address:", null, ConsoleColor.Yellow);
        var desc = Prompt.GetString("Description:", null, ConsoleColor.White);

        var addressBook = new AddressBook
        {
            Created = DateTime.UtcNow,
            Description = desc,
            Name = name,
            RecipientAddress = address
        };

        var result = _commandReceiver.AddAddressBook(activeSession, ref addressBook);
        _console.WriteLine(result.Item1 == null ? result.Item2 : $"Saved {name} to your address book.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="activeSession"></param>
    private void Update(Session activeSession)
    {
        var address = Prompt.GetString("Address/Name:", null, ConsoleColor.Green);
        var addressBook = new AddressBook { Name = address };
        var result = _commandReceiver.FindAddressBook(activeSession, ref addressBook);
        if (result.Item1 != null)
        {
            addressBook = result.Item1 as AddressBook;
            var table = new ConsoleTable("Created", "Name", "Address", "Description");
            table.AddRow(addressBook.Created.ToString("dd-MM-yyyy HH:mm:ss"), addressBook.Name,
                addressBook.RecipientAddress, addressBook.Description);
            table.Configure(o => o.NumberAlignment = Alignment.Right);
            _console.WriteLine($"\n{table}");
            _console.WriteLine("\n");

            var newName = Prompt.GetString("Name:", null, ConsoleColor.Yellow);
            var newAddress = Prompt.GetString("Address:", null, ConsoleColor.Red);
            var newDesc = Prompt.GetString("Description:", null, ConsoleColor.Gray);

            if (!string.IsNullOrEmpty(newName))
            {
                if (addressBook.Name != newName)
                {
                    addressBook.Name = newName;
                }
            }

            if (!string.IsNullOrEmpty(newAddress))
            {
                if (addressBook.RecipientAddress != newAddress)
                {
                    addressBook.RecipientAddress = newAddress;
                }
            }

            if (!string.IsNullOrEmpty(newDesc))
            {
                if (addressBook.Description != newDesc)
                {
                    addressBook.Description = newDesc;
                }
            }

            var resultUpdate = _commandReceiver.AddAddressBook(activeSession, ref addressBook, true);
            _console.WriteLine(result.Item1 == null ? resultUpdate.Item2 : $"Saved {newName} to your address book.");
        }
        else
        {
            _console.WriteLine(result.Item2);
        }
    }
}