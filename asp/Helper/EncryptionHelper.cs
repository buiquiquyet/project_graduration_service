using System.Security.Cryptography;
using System.Text;
using System;
using System.IO;
namespace asp.Helper
{
    public class EncryptionHelper
    {
        private const string secretKey = "secretKey_project_graduration"; // key login encode password
        
        public string Decrypt(string passWord)
        {
            // Tách IV và ciphertext
            string[] parts = passWord.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Cipher text format is incorrect. Expected format is <IV>:<CipherText>.");
            }

            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] cipher = Convert.FromBase64String(parts[1]);

            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(secretKey);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(cipher))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd(); // Trả về mật khẩu đã giải mã
                }
            }
        }
    }
}

