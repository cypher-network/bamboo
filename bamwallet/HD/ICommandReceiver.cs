// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Security;
using BAMWallet.Model;
using NBitcoin;
using System;
using System.Threading.Tasks;
using BAMWallet.Helper;
using NBitcoin.Stealth;
using Transaction = BAMWallet.Model.Transaction;

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
        Tuple<object, string> RecoverTransactions(in Session session, int start, bool recoverCompletely = false);
        Task SyncWallet(in Session session);
        Task<MessageResponse<StakeCredentialsResponse>> SendStakeCredentials(
            in StakeCredentialsRequest stakeCredentialsRequest, in byte[] privateKey, in byte[] token,
            in Output[] outputs);
        bool IsBase58(string address);
        Tuple<object, string> NotFoundTransactions(in Session session);
        Balance[] GetBalances(in Session session);
        Tuple<object, string> AddAddressBook(Session session, ref AddressBook addressBook, bool update = false);
        Tuple<object, string> FindAddressBook(Session session, ref AddressBook addressBook);
        Tuple<object, string> RemoveAddressBook(Session session, ref AddressBook addressBook);
        Tuple<object, string> ListAddressBook(Session session);
        ulong GetLastTransactionHeight(in Session session);
        Balance[] GetBalancesByTransactionId(in Session session, in byte[] transactionId);
        TaskResult<bool> RollBackTransaction(in Session session, Guid id);
        TaskResult<bool> GetSpending(Session session, WalletTransaction walletTransaction);
        PubKey ScanPublicKey(string address);
        (PubKey, StealthPayment) StealthPayment(string address);
        TaskResult<bool> Update<T>(Session session, T data);
        Transaction[] ReadWalletTransactions(in Session session);
        void SetNetworkSettings();
        BalanceProfile GetBalanceProfile(in Session session, Balance[] balanceArray = null);
        NetworkSettings NetworkSettings();
        ulong GetLastKnownStakeAmount(in Session session);
        bool SaveLastKnownStakeAmount(in Session session, ulong stakeAmount);
    }
}