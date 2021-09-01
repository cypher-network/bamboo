// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Security;
using BAMWallet.Model;
using NBitcoin;
using System;

namespace BAMWallet.HD
{
    public interface ICommandReceiver
    {
        string CreateWallet(in SecureString seed, in SecureString passphrase);
        string[] CreateSeed(in WordCount wordCount);
        Tuple<object, string> WalletList();
        Tuple<object, string> History(in Session session);
        Tuple<object, string> Address(in Session session);
        Tuple<object, string> Send(in Session session, ref WalletTransaction transaction);
        Tuple<object, string> ReceivePayment(in Session session, string paymentId);
        Tuple<object, string> CreateTransaction(Session session, ref WalletTransaction transaction);
        Tuple<object, string> RecoverTransactions(in Session session, int start);
        void SyncWallet(in Session session);
        bool IsTransactionAllowed(in Session session);
    }
}