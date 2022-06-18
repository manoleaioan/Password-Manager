using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Password_Manager
{
    class Aes
    {
        public static byte[] Encrypt(string plainText, string pw)
        {
            byte[] passwordBytes = Encoding.ASCII.GetBytes(pw);
            byte[] Key = SHA256Managed.Create().ComputeHash(passwordBytes);
            byte[] IV = MD5.Create().ComputeHash(passwordBytes);
            byte[] encrypted;

            using (AesManaged aes = new AesManaged())
            {
                ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(plainText);
                        encrypted = ms.ToArray();
                    }
                }
            }

            return encrypted;
        }
        public static string Decrypt(byte[] cipherText, string pw)
        {
            byte[] passwordBytes = Encoding.ASCII.GetBytes(pw);
            byte[] Key = SHA256Managed.Create().ComputeHash(passwordBytes);
            byte[] IV = MD5.Create().ComputeHash(passwordBytes);
            string plaintext = null;

            using (AesManaged aes = new AesManaged())
            {
                ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cs))
                            plaintext = reader.ReadToEnd();
                    }
                }
            }

            return plaintext;
        }

    }
}