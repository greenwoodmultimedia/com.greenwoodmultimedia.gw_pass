using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace gw_pass.Classes
{
    class GestionAes
    {
        private SecureString cle;
        private byte[] sel;
        private SecureString secureString;

        public GestionAes(SecureString cle, byte[] sel)
        {
            this.cle = cle;
            this.sel = sel;
        }

        public GestionAes(SecureString secureString)
        {
            this.secureString = secureString;
        }

        /// <summary>
        /// Encrypte des données en utilisant l'algorithme AES en mode CBC.
        /// </summary>
        /// <param name="donnees">Données sous format "string" à encrypter.</param>
        /// <returns>Retourne les données sous forme encrypté et en format base 64.</returns>
        public string encrypter(string donnees)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(donnees);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(sha_256(cle), sel, 1000, HashAlgorithmName.SHA256);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    donnees = Convert.ToBase64String(ms.ToArray());
                }
            }
            return donnees;
        }


        /// <summary>
        /// Décrypte des données en utilisant l'algorithme AES en mode CBC.
        /// </summary>
        /// <param name="donnees">Données sous format "string" à décrypter.</param>
        /// <returns>Retourne les données décryptées et en format Unicode.</returns>
        public string decrypter(string donnees)
        {
            byte[] cipherBytes = Convert.FromBase64String(donnees);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(sha_256(cle), sel, 1000, HashAlgorithmName.SHA256);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    donnees = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return donnees;
        }

        /// <summary>
        /// Hash un mot de passe en sha256.
        /// </summary>
        /// <param name="text">Le mot de passe à hashé sous forme de Secure String.</param>
        /// <returns>Retourne le hash.</returns>
        public static string sha_256(SecureString mot_de_passe)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(new NetworkCredential(string.Empty, mot_de_passe).Password);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }
    }
}
