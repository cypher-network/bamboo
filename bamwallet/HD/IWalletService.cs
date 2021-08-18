// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Security;
using BAMWallet.Model;
using NBitcoin;
using System;

namespace BAMWallet.HD
{
    public interface IWalletService
    {
        bool IsCommandExecutionInProgress { get; }
        string CreateWallet(SecureString seed, SecureString passphrase);
        string[] CreateSeed(Language language, WordCount wordCount);
        Tuple<object, string> WalletList();
        Tuple<object, string> History(Session session);
        Tuple<object, string> Address(Session session);
        Tuple<object, string> Send(Session session, ref WalletTransaction transaction);
        Tuple<object, string> ReceivePayment(Session session, string paymentId);
        Tuple<object, string> CreateTransaction(Session session, ref WalletTransaction transaction);
        Tuple<object, string> RecoverTransactions(Session session, int start);
        void SyncWallet(Session session);
    }
}