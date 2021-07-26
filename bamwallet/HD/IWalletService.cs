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
        TaskResult<bool> CalculateChange();
        TaskResult<IEnumerable<string>> WalletList();
        TaskResult<BalanceSheet[]> History();
        TaskResult<string> Address();
        KeySet KeySet();
        (PubKey, StealthPayment) StealthPayment(string address);
        int Count();
        (Key, Key) Unlock();
        Client HttpClient();
        TaskResult<bool> Save<T>(T data);
        Task<TaskResult<bool>> Send();
        Task<TaskResult<WalletTransaction>> ReceivePayment(string paymentId);
        TaskResult<WalletTransaction> CreateTransaction(Guid sessionId);
        PubKey ScanPublicKey(string address);
        Transaction GetTransaction(Guid sessionId);
        byte[] GetKeyImage(Vout output);
        TaskResult<bool> RollBackTransaction(Guid sessionId);
        Task SyncWallet(int n = 3);
        Task<TaskResult<bool>> RecoverTransactions(int start);
    }
}