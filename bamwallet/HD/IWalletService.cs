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
        TaskResult<ulong> AvailableBalance(Guid sessionId);
        void AddKeySet(Guid sessionId);
        KeySet CreateKeySet(KeyPath keyPath, byte[] secretKey, byte[] chainCode);
        string CreateWallet(SecureString mnemonic, SecureString passphrase);
        SecureString NewID(int bytes = 32);
        Task<string[]> CreateMnemonic(Language language, WordCount wordCount);
        ulong TotalAmount(Guid sessionId, string address);
        WalletTransaction LastTransaction(Guid sessionId, WalletType transactionType);
        TaskResult<WalletTransaction> SortChange(Guid sessionId);
        IEnumerable<string> WalletList();
        IEnumerable<BlanceSheet> History(Guid sessionId);
        IEnumerable<string> Addresses(Guid sessionId);
        IEnumerable<KeySet> KeySets(Guid sessionId);
        KeySet LastKeySet(Guid sessionId);
        KeySet GetKeySet(Guid sessionId);
        (PubKey, StealthPayment) MakeStealthPayment(string address);
        int Count(Guid sessionId);
        KeySet NextKeySet(Guid sessionId);
        (Key, Key) Unlock(Guid sessionId);
        Session GetSession(Guid sessionId);
        Client HttpClient();
        Session SessionAddOrUpdate(Session session);
        TaskResult<bool> Save(Guid sessionId, WalletTransaction walletTx);
        Task TransferPayment(Guid sessionId);
        Task ReceivePayment(Guid sessionId, string paymentId);
        TaskResult<Transaction> CreateTransaction(Session session, WalletTransaction walletTx);
        ulong Fee(int nByte);
        PubKey GetScanPublicKey(string address);
    }
}