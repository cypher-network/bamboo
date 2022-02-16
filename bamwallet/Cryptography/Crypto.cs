// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using Libsecp256k1Zkp.Net;
using NitraLibSodium.Box;
using NitraLibSodium.Aead;

namespace BAMWallet.Cryptography;

public static class Crypto
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <param name="associatedData"></param>
    /// <param name="tag"></param>
    /// <param name="nonce"></param>
    /// <returns></returns>
    public static byte[] EncryptChaCha20Poly1305(byte[] data, byte[] key, byte[] associatedData, out byte[] tag,
        out byte[] nonce)
    {
        tag = new byte[Chacha20poly1305.Abytes()];
        nonce = GetRandomData();
        var cipherText = new byte[data.Length + (int)Chacha20poly1305.Abytes()];
        var cipherTextLength = 0ul;
        return Chacha20poly1305.Encrypt(cipherText, ref cipherTextLength, data, (ulong)data.Length,
            associatedData, (ulong)associatedData.Length, null, nonce, key) != 0
            ? Array.Empty<byte>()
            : cipherText;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <param name="associatedData"></param>
    /// <param name="tag"></param>
    /// <param name="nonce"></param>
    /// <returns></returns>
    public static byte[] DecryptChaCha20Poly1305(byte[] data, byte[] key, byte[] associatedData, byte[] tag,
        byte[] nonce)
    {
        var decryptedData = new byte[data.Length];
        var decryptedDataLength = 0ul;
        return Chacha20poly1305.Decrypt(decryptedData, ref decryptedDataLength, null, data,
            (ulong)data.Length, associatedData, (ulong)associatedData.Length, nonce, key) != 0
            ? Array.Empty<byte>()
            : decryptedData;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cipher"></param>
    /// <param name="secretKey"></param>
    /// <param name="publicKey"></param>
    /// <returns></returns>
    public static byte[] BoxSealOpen(byte[] cipher, byte[] secretKey, byte[] publicKey)
    {
        var msg = new byte[cipher.Length];
        return Box.SealOpen(msg, cipher, (ulong)cipher.Length, publicKey, secretKey) != 0
            ? Array.Empty<byte>()
            : msg;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="publicKey"></param>
    /// <returns></returns>
    public static byte[] BoxSeal(byte[] msg, byte[] publicKey)
    {
        var cipher = new byte[msg.Length + (int)Box.Sealbytes()];
        return Box.Seal(cipher, msg, (ulong)msg.Length, publicKey) != 0
            ? Array.Empty<byte>()
            : cipher;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static (byte[] pk, byte[] sk) KeyPair()
    {
        var sk = new byte[Box.Secretkeybytes()];
        var pk = new byte[Box.Publickeybytes()];
        Box.Keypair(pk, sk);
        return (pk, sk);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private static byte[] GetRandomData()
    {
        using var secp256K1 = new Secp256k1();
        return secp256K1.RandomSeed();
    }
}