using System;
using System.IO;
using System.Security;
using CipherVault.Core.Helpers;
using CipherVault.Core.Records;
using Microsoft.Extensions.Logging;

namespace Decrypter;
internal class DecryptionService
{
    internal void DecryptFile(string zipFilePath, ILogger logger)
    {
        try
        {
            var sanityCheck = new WhileLoopSanityCheck();
            SecureString? secureFilePassword = new SecureString();

            // Validate the zip file path
            Console.WriteLine("Please enter the path to the encrypted zip file:");
            while (!FileHelper.ValidateZipFileExists(zipFilePath, logger))
            {
                Console.WriteLine("Zip file path is invalid. Please try again.");
                zipFilePath = Console.ReadLine();

                sanityCheck.ValidateAttempts();
            }

            secureFilePassword = PasswordHelper.ProcessPassewordInput("Please enter the password to encrypt the file.", logger);
            var filePassword = PasswordHelper.ConvertFromnSecureString(secureFilePassword, logger);

            var destinationFolder = $"{Path.GetDirectoryName(zipFilePath)}_decrypted";

            FileHelper.ExtractEncryptedZip(zipFilePath, destinationFolder, filePassword, logger);
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while decrypting the file: {ex}");
            throw;
        }
    }
}
