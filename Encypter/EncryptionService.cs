using Encypter.Data;
using Encypter.Helpers;
using Encypter.Records;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Encypter
{
    internal class EncryptionService
    {
        public void Start(ILogger logger, Validate? validate)
        {
            try
            {
                var sanityCheck = new WhileLoopSanityCheck();
                SecureString? secureFilePassword = new SecureString();

                Console.WriteLine("Please add the file path to the folder you want to encrypt?");
                var filePath = Console.ReadLine();

                while (!FileHelper.ValidateFileExists(filePath))
                {
                    Console.WriteLine("File path is invalid. Please try again.");
                    filePath = Console.ReadLine();

                    sanityCheck.ValidateAttempts();
                }

                secureFilePassword = PasswordHelper.ProcessPassewordInput("Please enter the password to encrypt the file. \n This must be 10 Charaters Long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.", logger);
                var filePassword = PasswordHelper.ConvertFromnSecureString(secureFilePassword, logger);

                if (validate.ValidatePassword(filePassword))
                {
                    logger.LogInformation("File Password is valid. \n Starting to process file.");

                    //Dispose of File Password
                    filePassword = string.Empty;
                    //Dispose of secureFilePassword
                    secureFilePassword.Dispose();
                }

                //EncryptFiles(filePath, password, masterPassword);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in process: {ex.Message}");
                throw;
            }
        }

        private void EncryptFiles(string filePath, string password, ILogger logger)
        {

        }

        //private void EncryptFiles(string filePath, string password, string masterPassword)
        //{
        //    var encrypt = new Encrypt();

        //    // Send Master Password and Files Passwords to be encrypted
        //    var encryptedPassword = encrypt.ProcessFilePassword(password);
        //    var encryptedMasterPassword = encrypt.ProcessFilePassword(masterPassword);

        //    if (encrypt.ProcessEncryptFolder(encryptedMasterPassword, encryptedPassword, masterPassword, password, filePath))
        //    {
        //        Console.WriteLine("Files encrypted successfully.");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Failed to encrypt files.");
        //    }
        //}
    }
}
