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

    /// <summary>
    /// Packages <paramref name="sourceFolder"/> into a plain (unencrypted) zip
    /// container at "{sourceFolder}.zip". Confidentiality and integrity are
    /// provided by the per-file AES-GCM .cwe layer; the zip is purely structural.
    /// </summary>
    public static void CreateZip(string sourceFolder, ILogger logger)
    {
        var zipPath = $"{sourceFolder}.zip";
        using (ZipFile zip = new ZipFile())
        {
            zip.AddDirectory(sourceFolder);
            zip.Save(zipPath);
        }

        logger.LogInformation("Created archive: {ZipPath}", zipPath);
    }

    public static void ExtractZip(string zipFilePath, string destinationFolder, ILogger logger)
    {
        using (ZipFile zip = ZipFile.Read(zipFilePath))
        {
            zip.ExtractAll(destinationFolder, ExtractExistingFileAction.OverwriteSilently);
        }

        logger.LogInformation("Extracted archive: {ZipPath}", zipFilePath);
    }

    public static void DeleteDirectory(string directoryPath, ILogger logger)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
            logger.LogInformation("Deleted directory: {DirectoryPath}", directoryPath);
        }
        else
        {
            logger.LogInformation("Directory does not exist: {DirectoryPath}", directoryPath);
        }
    }
}
