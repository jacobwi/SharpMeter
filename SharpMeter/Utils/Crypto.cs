using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharpMeter.Utils
{
    /// <summary>
    /// Cryptography class.
    /// </summary>
    public static class Crypto
    {
        #region Settings

        private static int _iterations = 2;
        private static int _keySize = 256;

        private static string _hash = "SHA1";
        private static string _salt = "aselrias38490a32"; // Random
        private static string _vector = "8947az34awl34kjq"; // Random

        #endregion Settings

        /// <summary>
        /// Encrypts the.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="password">The password.</param>
        /// <returns>A string.</returns>
        public static string Encrypt(string value, string password)
        {
            return Encrypt<AesManaged>(value, password);
        }

        /// <summary>
        /// Encrypts a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="password">The password.</param>
        /// <returns>A string.</returns>
        private static string Encrypt<T>(string value, string password)
                where T : SymmetricAlgorithm, new()
        {
            var vectorBytes = Encoding.UTF8.GetBytes(_vector);
            var saltBytes = Encoding.ASCII.GetBytes(_salt);
            var valueBytes = Encoding.UTF8.GetBytes(value);

            byte[] encrypted;
            using (var cipher = new T())
            {
                var _passwordBytes =
                    new PasswordDeriveBytes(password, saltBytes, _hash, _iterations);
                var keyBytes = _passwordBytes.GetBytes(_keySize / 8);

                cipher.Mode = CipherMode.CBC;

                using (var encryptor = cipher.CreateEncryptor(keyBytes, vectorBytes))
                {
                    using (var to = new MemoryStream())
                    {
                        using (var writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write))
                        {
                            writer.Write(valueBytes, 0, valueBytes.Length);
                            writer.FlushFinalBlock();
                            encrypted = to.ToArray();
                        }
                    }
                }
                cipher.Clear();
            }
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Decrypts a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="password">The password.</param>
        /// <returns>A string.</returns>
        public static string Decrypt(string value, string password)
        {
            return Decrypt<AesManaged>(value, password);
        }

        /// <summary>
        /// Decrypts a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="password">The password.</param>
        /// <returns>A string.</returns>
        private static string Decrypt<T>(string value, string password) where T : SymmetricAlgorithm, new()
        {
            var vectorBytes = Encoding.ASCII.GetBytes(_vector);
            var saltBytes = Encoding.ASCII.GetBytes(_salt);
            var valueBytes = Convert.FromBase64String(value);

            byte[] decrypted;
            var decryptedByteCount = 0;

            using (var cipher = new T())
            {
                var _passwordBytes = new PasswordDeriveBytes(password, saltBytes, _hash, _iterations);
                var keyBytes = _passwordBytes.GetBytes(_keySize / 8);

                cipher.Mode = CipherMode.CBC;

                try
                {
                    using var decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes);
                    using var from = new MemoryStream(valueBytes);
                    using var reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read);
                    decrypted = new byte[valueBytes.Length];
                    decryptedByteCount = reader.Read(decrypted, 0, decrypted.Length);
                }
                catch (Exception ex)
                {
                    return String.Empty;
                }

                cipher.Clear();
            }
            return Encoding.UTF8.GetString(decrypted, 0, decryptedByteCount);
        }
    }
}