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
        public string Note { get; set; }
        public static string Hash(String _language, string _source)
        {
            return Hash(_language, _source, null);
        }
        /// <summary>
        /// Hashes a translation key. A developer note changes the expected translation,
        /// so it becomes part of the key - texts without a note keep their original hash.
        /// </summary>
        public static string Hash(String _language, string _source, string _note)
        {
            string key = _language + '.' + _source;
            if (!string.IsNullOrWhiteSpace(_note))
                key = key + '.' + _note.Trim();
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(key));

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
