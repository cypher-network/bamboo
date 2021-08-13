// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using NBitcoin;
using BAMWallet.Helper;
using BAMWallet.Model;
using Transaction = BAMWallet.Model.Transaction;
using System;

namespace BAMWallet.HD
{
    public interface IWalletService
    {
        bool IsCommandExecutionInProgress { get; }
        string CreateWallet(SecureString seed, SecureString passphrase);
        string[] CreateSeed(Language language, WordCount wordCount);
        TaskResult<IEnumerable<string>> WalletList();
        TaskResult<BalanceSheet[]> History(Session session);
        TaskResult<string> Address(Session session);
        Tuple<bool, string> Send(Session session);
        TaskResult<WalletTransaction> ReceivePayment(Session session, string paymentId);
        TaskResult<WalletTransaction> CreateTransaction(Session session);
        Transaction GetTransaction(Session session);
        Tuple<bool, string> RecoverTransactions(Session session, int start);
        void SyncWallet(Session session);
    }
}