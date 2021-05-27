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
        void AddKeySet(Guid sessionId);
        KeySet CreateKeySet(KeyPath keyPath, byte[] secretKey, byte[] chainCode);
        string CreateWallet(SecureString mnemonic, SecureString passphrase);
        SecureString NewId(int bytes = 32);
        Task<string[]> CreateMnemonic(Language language, WordCount wordCount);
        WalletTransaction LastWalletTransaction(Guid sessionId, WalletType transactionType);
        TaskResult<bool> CalculateChange(Guid sessionId);
        TaskResult<IEnumerable<string>> WalletList();
        TaskResult<BalanceSheet[]> History(Guid sessionId);
        TaskResult<IEnumerable<string>> Addresses(Guid sessionId);
        IEnumerable<KeySet> KeySets(Guid sessionId);
        KeySet LastKeySet(Guid sessionId);
        KeySet KeySet(Guid sessionId);
        (PubKey, StealthPayment) StealthPayment(string address);
        int Count(Guid sessionId);
        KeySet NextKeySet(Guid sessionId);
        (Key, Key) Unlock(Guid sessionId);
        Session Session(Guid sessionId);
        Client HttpClient();
        Session SessionAddOrUpdate(Session session);
        TaskResult<bool> Save<T>(Guid sessionId, T data);
        Task<TaskResult<bool>> Send(Guid sessionId);
        Task<TaskResult<WalletTransaction>> ReceivePayment(Guid sessionId, string paymentId);
        TaskResult<WalletTransaction> CreateTransaction(Guid sessionId);
        ulong Fee(int nByte);
        PubKey ScanPublicKey(string address);
        Transaction GetTransaction(Guid sessionId);
        public byte[] GetKeyImage(Guid sessionId, Vout output);
    }
}