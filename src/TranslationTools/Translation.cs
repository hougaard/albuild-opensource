using System;
using System.Text;
using System.Security.Cryptography;

namespace TranslationTools
{
    public class Translation
    {
        public int Id { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public string Index { get; set; }
        public string Language { get; set; }
        public string Origin { get; set; }
        public static string Hash(String _language, string _source)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(_language + '.' + _source));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public override string ToString()
        {
            return Language + " : " + source + " -> " + target;
        }
    }
}
