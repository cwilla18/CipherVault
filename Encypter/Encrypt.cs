using Encypter.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Encypter
{
    internal sealed class Encrypt
    {
        internal string ProcessFilePassword(string password)
        {
            return ProcessPassword(password);
        }

        internal bool ProcessEncryptFolder(string encryptedMasterPassword, string encryptedFilePassword, string masterPassword, string filePassword, string filePath)
        {
            return EncryptFolder(encryptedMasterPassword, encryptedFilePassword, masterPassword, filePassword, filePath);
        }

        private string ProcessPassword(string password)
        {
            try
            {
                var changedPassword = string.Empty;

                // Split the password into 4 equal parts
                var passwordSplit = password.Length / Config.ChunkingSize;

                // Handle the case where the password length is not perfectly divisible by 4
                String[] passSplit = Enumerable.Range(0, 4).Select(i => i == 3 ? password.Substring(i * passwordSplit) : password.Substring(i * passwordSplit, passwordSplit)).ToArray();

                // Process each part of the password
                for (int i = 0; i < passSplit.Length; i++)
                {
                    var items = passSplit[i].ToCharArray();
                    var charInc = (i + Config.Increment);
                    var updatedValues = items.Select(c => (char)(c + charInc)).ToArray();
                    changedPassword += new string(updatedValues);
                }

                return changedPassword;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing password: {ex.Message}", ex);
            }
        }

        private bool EncryptFolder(string encryptedMasterPassword, string encryptedFilePassword, string masterPassword, string filePassword, string filePath)
        {
            try
            {
                if (AddCweEncryptionToFile(filePath, encryptedMasterPassword, encryptedFilePassword))
                {
                    EncryptFolder(filePath, masterPassword, filePassword);

                    return true;
                }

                Console.WriteLine($"Encrypting folder: {filePath} with master password and file password.");
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error encrypting folder: {ex.Message}", ex);
            }
        }

        private bool AddCweEncryptionToFile(string filePath, string encryptedMasterPassword, string encryptedFilePassword)
        {
            try
            {
                
                if (Directory.Exists(filePath))
                {
                    //Set path for data.cwe
                    var path = $"{filePath}{Config.FileTypePath}";

                    //Write the encrypted master password and file password to the data.cwe file
                    var data = $"{encryptedMasterPassword} [[CW-E18]] {encryptedFilePassword}";
                    var binaryData = Encoding.UTF8.GetBytes(data);

                    //Save binary data and set file as hidden
                    File.WriteAllBytes(path, binaryData);
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding encryption to file: {ex.Message}", ex);
            }
        }

        private void EncryptFolder(string filePath, string masterPassword, string filePassword)
        {
            var salt = Encoding.UTF8.GetBytes(masterPassword);
            using var deriveBytes = new Rfc2898DeriveBytes(filePassword, salt, 100000, HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(Config.KeySize);

            foreach (string file in Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories))
            {
                EncryptFile(file, key);
            }
        }

        private void EncryptFile(string filePath, byte[] key)
        {
            var attributes = File.GetAttributes(filePath);

            if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                return;
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV(); // Generates a unique IV for this file

            using var ms = new MemoryStream();
            // 1. Write the IV to the beginning of the file so decryption knows what it is
            ms.Write(aes.IV, 0, aes.IV.Length);

            // 2. Encrypt the file contents
            using (var encryptor = aes.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(fileBytes, 0, fileBytes.Length);
                cryptoStream.FlushFinalBlock();
            }

            // 3. Overwrite the original file with the encrypted payload
            File.WriteAllBytes(filePath, ms.ToArray());

            // Optional: Append a custom extension to show it's encrypted
            File.Move(filePath, filePath + ".cwe");
        }
    }
}
