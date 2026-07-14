using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Ionic.Zip;

namespace CipherVault.Core.Helpers;

public static class FileHelper
{
    public static bool ValidateZipFileExists(string? filePath, ILogger logger) => !string.IsNullOrEmpty(filePath) && File.Exists(filePath);

    public static bool ValidateFileExists(string? filePath, ILogger logger)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            logger.LogInformation("File path cannot be empty.");
            return false;
        }
        if (!Directory.Exists(filePath))
        {
            logger.LogInformation("The specified directory does not exist.");
            return false;
        }
        return true;
    }

    public static List<string> GetFilesFromDirectory(string filePath)
    {
        return Directory.GetFiles(filePath, "*", SearchOption.AllDirectories).ToList();
    }

    public static void CreateEncryptedZip(string sourceFolder, string zipPath, string password, ILogger logger)
    {
        using (ZipFile zip = new ZipFile())
        {
            zip.Password = password;
            zip.Encryption = EncryptionAlgorithm.WinZipAes256;
            zip.AddDirectory(zipPath);
            zip.Save($"{sourceFolder}.zip");
        }

        logger.LogInformation($"Created password-protected ZIP: {zipPath}");
    }

    public static void ExtractEncryptedZip(string zipFilePath, string destinationFolder, string password, ILogger logger)
    {
        using (ZipFile zip = ZipFile.Read(zipFilePath))
        {
            zip.Password = password;
            zip.ExtractAll(destinationFolder, ExtractExistingFileAction.OverwriteSilently);
        }

        logger.LogInformation($"Extracted password-protected ZIP: {zipFilePath}");
    }

    public static void DeleteDirectory(string directoryPath, ILogger logger)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
            logger.LogInformation($"Deleted directory: {directoryPath}");
        }
        else
        {
            logger.LogInformation($"Directory does not exist: {directoryPath}");
        }
    }
}
