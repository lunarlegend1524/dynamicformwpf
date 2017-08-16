// -----------------------------------------------------------------------
// <copyright file="TDEAEncryption.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DynamicFormWPF.Classes_Data
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DataEncryption
    {
        ///<summary>
        /// Steve Lydford - 12/05/2008.
        ///
        /// Encrypts a file using Rijndael algorithm.
        ///</summary>
        ///<param name="inputFile"></param>
        ///<param name="outputFile"></param>
        ///http://www.codeproject.com/Articles/26085/File-Encryption-and-Decryption-in-C
        public static string EncryptFile(string inputFile, string outputFile)
        {
            string info = string.Empty;
            try
            {
                // to prevent Specified initialization vector (IV) does not match the block size for this algorithm exception
                // the initial vector size needs to be 16 bytes equals 8 characters
                string password = "SEIS_123"; // Your Key Here
                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(password);

                string cryptFile = outputFile;
                FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateEncryptor(key, key),
                    CryptoStreamMode.Write);

                FileStream fsIn = new FileStream(inputFile, FileMode.Open);

                int data;
                while ((data = fsIn.ReadByte()) != -1)
                    cs.WriteByte((byte)data);

                fsIn.Close();
                cs.Close();
                fsCrypt.Close();
                info = "OK";
            }
            catch
            {
                info = "Encryption failed!";
            }
            return info;
        }

        ///<summary>
        /// Steve Lydford - 12/05/2008.
        ///
        /// Decrypts a file using Rijndael algorithm.
        ///</summary>
        ///<param name="inputFile"></param>
        ///<param name="outputFile"></param>
        public static string DecryptFile(string inputFile, string outputFile)
        {
            string password = "SEIS_123"; // Your Key Here
            string info = string.Empty;

            UnicodeEncoding UE = new UnicodeEncoding();

            try
            {
                byte[] key = UE.GetBytes(password);

                FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateDecryptor(key, key),
                    CryptoStreamMode.Read);

                FileStream fsOut = new FileStream(outputFile, FileMode.Create);

                int data;
                while ((data = cs.ReadByte()) != -1)
                    fsOut.WriteByte((byte)data);

                fsOut.Close();
                cs.Close();
                fsCrypt.Close();
                info = "OK";
            }
            catch
            {
                info = "Decryption failed!";
            }
            return info;
        }

        /// By Syed Moshiur Murshed, 18 May 2006
        /// tripleDES method - Triple Data Encryption Algorithm (TDEA or Triple DEA) block cipher,
        /// which applies the Data Encryption Standard (DES) cipher algorithm three times to each data block.
        /// http://www.codeproject.com/Articles/14150/Encrypt-and-Decrypt-Data-with-C
        public static string Encrypt(string toEncrypt, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            // the key to encrypt
            string key = "SEIS";

            //If hashing use get hashcode regards to your key
            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));

                //Always release the resources and flush data
                // of the Cryptographic service provide. Best Practice

                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;

            //mode of operation. there are other 4 modes.
            //We choose ECB(Electronic code Book)
            tdes.Mode = CipherMode.ECB;

            //padding mode(if any extra byte added)

            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();

            //transform the specified region of bytes array to resultArray
            byte[] resultArray =
              cTransform.TransformFinalBlock(toEncryptArray, 0,
              toEncryptArray.Length);

            //Release resources held by TripleDes Encryptor
            tdes.Clear();

            //Return the encrypted data into unreadable string format
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        /// By Syed Moshiur Murshed, 18 May 2006
        /// tripleDES method - Triple Data Encryption Algorithm (TDEA or Triple DEA) block cipher,
        /// which applies the Data Encryption Standard (DES) cipher algorithm three times to each data block.
        /// http://www.codeproject.com/Articles/14150/Encrypt-and-Decrypt-Data-with-C
        public static string Decrypt(string cipherString, bool useHashing)
        {
            string result = string.Empty;

            try
            {
                byte[] keyArray;

                //get the byte code of the string

                byte[] toEncryptArray = Convert.FromBase64String(cipherString);

                // the key to decrypt

                string key = "SEIS";

                if (useHashing)
                {
                    //if hashing was used get the hash code with regards to your key
                    MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                    keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));

                    //release any resource held by the MD5CryptoServiceProvider

                    hashmd5.Clear();
                }
                else
                {
                    //if hashing was not implemented get the byte code of the key
                    keyArray = UTF8Encoding.UTF8.GetBytes(key);
                }

                TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

                //set the secret key for the tripleDES algorithm
                tdes.Key = keyArray;

                //mode of operation. there are other 4 modes.
                //We choose ECB(Electronic code Book)

                tdes.Mode = CipherMode.ECB;

                //padding mode(if any extra byte added)
                tdes.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = tdes.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(
                                     toEncryptArray, 0, toEncryptArray.Length);

                //Release resources held by TripleDes Encryptor
                tdes.Clear();

                //return the Clear decrypted TEXT
                result = UTF8Encoding.UTF8.GetString(resultArray);
            }
            catch (Exception)
            {
                result = "null";
            }
            return result;
        }
    }
}