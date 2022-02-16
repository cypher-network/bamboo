// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Security;
using BAMWallet.Model;
using NBitcoin;
using System;

namespace BAMWallet.HD
{
    public interface ICommandReceiver
    {
        Task<string> CreateWallet(in SecureString seed, in SecureString passphrase, in string walletName);
        string[] CreateSeed(in WordCount wordCount);
        Tuple<object, string> WalletList();
        Tuple<object, string> History(in Session session);
        Tuple<object, string> Address(in Session session);
        Tuple<object, string> SendTransaction(in Session session, ref WalletTransaction transaction);
        Tuple<object, string> ReceivePayment(in Session session, string paymentId);
        Tuple<object, string> CreateTransaction(Session session, ref WalletTransaction transaction);
        Tuple<object, string> RecoverTransactions(in Session session, int start);
        Task SyncWallet(in Session session);
        Task<MessageResponse<StakeCredentialsResponse>> SendStakeCredentials(in Session session,
            in StakeCredentialsRequest stakeCredentialsRequest, in byte[] privateKey, in byte[] token);
        Tuple<object, string> AddAddressBook(Session session, ref AddressBook addressBook, bool update = false);
        Tuple<object, string> FindAddressBook(Session session, ref AddressBook addressBook);
        Tuple<object, string> RemoveAddressBook(Session session, ref AddressBook addressBook);
        Tuple<object, string> ListAddressBook(Session session);
    }
}