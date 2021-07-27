// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Stealth;
using BAMWallet.Helper;
using BAMWallet.Rpc;
using BAMWallet.Model;
using Transaction = BAMWallet.Model.Transaction;

namespace BAMWallet.HD
{
    public interface IWalletService
    {
        KeySet CreateKeySet(KeyPath keyPath, byte[] secretKey, byte[] chainCode);
        string CreateWallet(SecureString mnemonic, SecureString passphrase);
        SecureString NewId(int bytes = 32);
        Task<string[]> CreateMnemonic(Language language, WordCount wordCount);
        TaskResult<bool> CalculateChange(Session session);
        TaskResult<IEnumerable<string>> WalletList();
        TaskResult<BalanceSheet[]> History(Session session);
        TaskResult<string> Address(Session session);
        KeySet KeySet(Session session);
        (PubKey, StealthPayment) StealthPayment(string address);
        int Count(Session session);
        (Key, Key) Unlock(Session session);
        Client HttpClient();
        TaskResult<bool> Save<T>(Session session, T data);
        Task<TaskResult<bool>> Send(Session session);
        Task<TaskResult<WalletTransaction>> ReceivePayment(Session session, string paymentId);
        TaskResult<WalletTransaction> CreateTransaction(Session session, Guid sessionId);
        PubKey ScanPublicKey(string address);
        Transaction GetTransaction(Session session);
        byte[] GetKeyImage(Session session, Vout output);
        TaskResult<bool> RollBackTransaction(Session session, Guid sessionId);
        Task SyncWallet(Session session, int n = 3);
        Task<TaskResult<bool>> RecoverTransactions(Session session, int start);
    }
}