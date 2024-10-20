using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace asp.Services
{
    public class KeyGenerator
    {
        public static string Generate256BitKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] keyBytes = new byte[32];

                rng.GetBytes(keyBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < keyBytes.Length; i++)
                {
                    sb.Append(keyBytes[i].ToString("x2")); 
                }

                return sb.ToString();
            }
        }
    }
}
