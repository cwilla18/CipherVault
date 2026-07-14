using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using CipherVault.Core.Data;
using CipherVault.Core.Formats;
using CipherVault.Core.Helpers;
using CipherVault.Core.Interfaces;
using CipherVault.Core.Records;
using CipherVault.Interfaces;
using Microsoft.Extensions.Logging;

namespace CipherVault;

internal class EncryptionService : IValidate
{
    private readonly IValidate _validate;
    private readonly ICipher _cipher;

    public EncryptionService(IValidate validate, ICipher cipher)
    {
        _validate = validate;
        _cipher = cipher;
    }

    public void Start(ILogger logger)
    {
        try
        {
            var sanityCheck = new WhileLoopSanityCheck();
            SecureString? secureFilePassword = new SecureString();

            Console.WriteLine("Please add the file path to the folder you want to encrypt?");
            var filePath = Console.ReadLine();

            while (!FileHelper.ValidateFileExists(filePath, logger))
            {
                Console.WriteLine("File path is invalid. Please try again.");
                filePath = Console.ReadLine();

                sanityCheck.ValidateAttempts();
            }

            secureFilePassword = PasswordHelper.ProcessPassewordInput("Please enter the password to encrypt the file. \n This must be 10 Charaters Long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.", logger);
            var filePassword = PasswordHelper.ConvertFromnSecureString(secureFilePassword, logger);

            if (_validate.ValidatePassword(filePassword))
            {
                logger.LogInformation("File Password is valid. \n Starting to process file.");

                var salt = RandomNumberGenerator.GetBytes(Config.SaltSize);

                // Derive encryption key from the password and the random salt.
                var encryptionKey = _cipher.DeriveKeyFromPassword(filePassword, salt, Config.KdfIterations);

                var copiedFilePath = CreateFolderAndCopyContent(filePath!, logger);

                // Now encrypt the file content
                EncryptFiles(copiedFilePath, encryptionKey, salt, logger);

                FileHelper.CreateEncryptedZip(copiedFilePath, copiedFilePath, filePassword, logger);

                FileHelper.DeleteDirectory(copiedFilePath, logger);

                //Dispose of File Password
                filePassword = string.Empty;
                //Dispose of secureFilePassword
                secureFilePassword.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error in process: {ex}");
            throw;
        }
    }

    private static string CreateFolderAndCopyContent(string sourceFilePath, ILogger logger)
    {
        var sourceName = $"{Path.GetFileName(sourceFilePath)}_encrypted";
        var destinationFolder = Path.Combine(sourceFilePath, sourceName);

        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        var dirctoryFiles = FileHelper.GetFilesFromDirectory(sourceFilePath);

        if (dirctoryFiles.Count == 0)
        {
            logger.LogInformation("No files found in the source directory.");
            return string.Empty;
        }

        foreach (var file in dirctoryFiles)
        {
            var destinationFilePath = Path.Combine(destinationFolder, Path.GetFileName(file));
            File.Copy(file, destinationFilePath, true);
        }

        return destinationFolder;
    }

    private void EncryptFiles(string filePath, byte[] encryptionKey, byte[] salt, ILogger logger)
    {
        var files = FileHelper.GetFilesFromDirectory(filePath);

        if (files.Count == 0)
        {
            throw new FileNotFoundException($"No files found in the specified path: {filePath}");
        }

        foreach (var file in files)
        {
            var attributes = File.GetAttributes(file);

            if (attributes.HasFlag(FileAttributes.Hidden))
            {
                continue; // Skip hidden files
            }

            var fileBytes = File.ReadAllBytes(file);

            if (fileBytes.Length == 0)
            {
                throw new Exception($"File {file} is empty");
            }

            // Encrypt the file content
            var encryptedPayload = _cipher.Encrypt(fileBytes, ReadOnlySpan<byte>.Empty, encryptionKey);

            var outputPath = file + ".cwe";
            using var outputStream = File.Create(outputPath);

            CweFileFormat.Write(outputStream, salt, Config.KdfIterations, encryptedPayload);

            logger.LogInformation($"File encrypted successfully: {outputPath}");
        }

        foreach (var file in files)
        {
            var attributes = File.GetAttributes(file);

            if (attributes.HasFlag(FileAttributes.Hidden))
            {
                continue; // Skip hidden files
            }

            File.Delete(file);
        }

        logger.LogInformation($"All files in the specified path have been encrypted successfully: {filePath}");
    }
}
