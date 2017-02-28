using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ACMC_Library_System.Supports
{
    public class Encryption
    {
        #region FILEDS

        private static readonly byte[] Keys = { 0x12, 0x19, 0x15, 0x17, 0x08, 0x09, 0xAF, 0xDF };

        #endregion

        #region METHODS

        /// <summary>
        /// DES string encryption
        /// </summary>
        /// <param name="encryptString">Input string</param>
        /// <param name="encryptKey">length 8 encryption key</param>
        /// <returns>Encrypted string</returns>
        public static string Encrypt(string encryptString, string encryptKey)
        {
            try
            {
                var rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                var rgbIv = Keys;
                var inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                var dCsp = new DESCryptoServiceProvider();
                var mStream = new MemoryStream();
                var cStream = new CryptoStream(mStream, dCsp.CreateEncryptor(rgbKey, rgbIv), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Convert.ToBase64String(mStream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }

        /// <summary>
        /// DES string decryption
        /// </summary>
        /// <param name="decryptString">Encrypted string</param>
        /// <param name="decryptKey">length 8 encryption key</param>
        /// <returns>Dencrypted string</returns>
        public static string Decrypt(string decryptString, string decryptKey)
        {
            try
            {
                var rgbKey = Encoding.UTF8.GetBytes(decryptKey);
                var rgbIv = Keys;
                var inputByteArray = Convert.FromBase64String(decryptString);
                var dcsp = new DESCryptoServiceProvider();
                var mStream = new MemoryStream();
                var cStream = new CryptoStream(mStream, dcsp.CreateDecryptor(rgbKey, rgbIv), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Encoding.UTF8.GetString(mStream.ToArray());
            }
            catch
            {
                return decryptString;
            }
        }

        #endregion
    }
}
