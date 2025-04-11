using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Damselfly.Core.Utils
{
    /// <summary>
    /// Utility class for encrypting and decrypting sensitive data like OAuth tokens
    /// </summary>
    public static class TokenEncryption
    {
        private static string Key; // 32 characters for AES-256
        private static string IV; // 16 characters for AES

        public static void Initialize(IConfiguration configuration)
        {
            // Optionally, you can load the Key and IV from configuration settings
            Key = configuration["Encryption:Key"] ?? throw new Exception("Encyrption Key not set!");
            IV = configuration["Encryption:IV"] ?? throw new Exception("Encryption IV not set!");
        }

        /// <summary>
        /// Encrypts a string value
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <returns>Base64 encoded encrypted string</returns>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.IV = Encoding.UTF8.GetBytes(IV);

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            using( var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write) )
            using( var swEncrypt = new StreamWriter(csEncrypt) )
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        /// <summary>
        /// Decrypts an encrypted string value
        /// </summary>
        /// <param name="cipherText">The base64 encoded encrypted text</param>
        /// <returns>The decrypted string</returns>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = Encoding.UTF8.GetBytes(IV);

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using var msDecrypt = new MemoryStream(cipherBytes);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                return srDecrypt.ReadToEnd();
            }
            catch (Exception)
            {
                // If decryption fails, return the original text (might be unencrypted)
                return cipherText;
            }
        }
    }
} 