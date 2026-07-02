using Encypter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Encypter
{
    internal class Process
    {
        public void Start(string masterPassword)
        {
            try
            {
                var validate = new Validate();

                Console.WriteLine("Please add the file path to the folder you want to encrypt?");
                var filePath = Console.ReadLine();

                int attempts = 0;
                while (!validate.ValidateFileExists(filePath))
                {
                    Console.WriteLine("File path is invalid. Please try again.");
                    filePath = Console.ReadLine();

                    attempts++;
                    if (attempts >= Config.WhileLoopSanityCheck)
                    {
                        throw new InvalidOperationException("Too many invalid attempts.");
                    }
                }

                Console.WriteLine("Please enter the password to encrypt the file:");
                var password = Console.ReadLine();

                password = validate.SetPassword(password);

                EncryptFiles(filePath, password, masterPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in process: {ex.Message}");
                throw;
            }
        }

        private void EncryptFiles(string filePath, string password, string masterPassword)
        {
            var encrypt = new Encrypt();

            // Send Master Password and Files Passwords to be encrypted
            var encryptedPassword = encrypt.ProcessFilePassword(password);
            var encryptedMasterPassword = encrypt.ProcessFilePassword(masterPassword);

            if (encrypt.ProcessEncryptFolder(encryptedMasterPassword, encryptedPassword, masterPassword, password, filePath))
            {
                Console.WriteLine("Files encrypted successfully.");
            }
            else
            {
                Console.WriteLine("Failed to encrypt files.");
            }
        }
    }
}
