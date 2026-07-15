using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CipherVault.Core.Formats;
using CipherVault.Core.Helpers;
using CipherVault.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Decrypter;
internal class DecryptionService
{
    private readonly ICipher _cipher;
    private readonly IConsoleUi _ui;

    public DecryptionService(ICipher cipher, IConsoleUi ui)
    {
        _cipher = cipher;
        _ui = ui;
    }

    internal void DecryptFile(ILogger logger)
    {
        try
        {
            _ui.Section("Select file");
            var zipFilePath = _ui.PromptExistingFile(
                "Enter the path to the encrypted .zip file:", ".zip");

            _ui.Section("Enter password");
            using var secure = _ui.ReadSecret("Enter the password used to encrypt the file:");

            var stopwatch = Stopwatch.StartNew();

            var destinationFolder = Path.Combine(
                new FileInfo(zipFilePath).Directory!.FullName,
                $"{Path.GetFileName(Path.GetDirectoryName(zipFilePath))}_decrypted");

            _ui.Section("Decrypting");
            _ui.Info("Extracting archive...");
            FileHelper.ExtractZip(zipFilePath, destinationFolder, logger);

            var cweFiles = GetCweFilesFromDirectory(destinationFolder);

            if (cweFiles.Count == 0)
            {
                _ui.Warn("No .cwe files were found inside the archive. Nothing to decrypt.");
                logger.LogWarning("No .cwe files found in {DestinationFolder}.", destinationFolder);
                return;
            }

            var decryptedCount = PasswordHelper.UsePasswordBytes(secure,
                pw => DecryptCweFiles(cweFiles, pw, logger));

            stopwatch.Stop();

            _ui.Summary("Decryption complete", new List<(string, string)>
            {
                ("Files decrypted", decryptedCount.ToString()),
                ("Output", destinationFolder),
                ("Elapsed", $"{stopwatch.Elapsed.TotalSeconds:F1}s"),
            });
            _ui.Success("Your files have been decrypted.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while decrypting.");
            _ui.Error("Decryption failed. Check the password and file, then try again.");
            throw;
        }
    }

    private List<string> GetCweFilesFromDirectory(string filePath)
    {
        return FileHelper.GetFilesFromDirectory(filePath).Where(w => w.EndsWith(".cwe")).ToList();
    }

    private int DecryptCweFiles(List<string> cweFiles, byte[] passwordBytes, ILogger logger)
    {
        // A vault shares one salt/iteration count, so cache derived keys rather
        // than paying the (deliberately expensive) KDF cost once per file.
        var keyCache = new Dictionary<string, byte[]>();
        var decrypted = 0;

        for (var i = 0; i < cweFiles.Count; i++)
        {
            var file = cweFiles[i];
            var attributes = File.GetAttributes(file);

            if (attributes.HasFlag(FileAttributes.Hidden))
            {
                continue; // Skip hidden files
            }

            var fileBytes = File.ReadAllBytes(file);

            using var inputStream = new MemoryStream(fileBytes);
            var cweFile = CweFileFormat.Read(inputStream);

            var cacheKey = $"{Convert.ToHexString(cweFile.Salt)}:{cweFile.Iterations}";
            if (!keyCache.TryGetValue(cacheKey, out var key))
            {
                key = _cipher.DeriveKeyFromPassword(passwordBytes, cweFile.Salt, cweFile.Iterations);
                keyCache[cacheKey] = key;
            }

            var associatedData = CweFileFormat.BuildAssociatedData(cweFile.Salt, cweFile.Iterations);
            var decryptedBytes = _cipher.Decrypt(cweFile.Payload, associatedData, key);

            var decryptedFilePath = file.Remove(file.Length - 4); // Remove the .cwe extension

            File.WriteAllBytes(decryptedFilePath, decryptedBytes);

            decrypted++;
            _ui.Progress(i + 1, cweFiles.Count, Path.GetFileName(decryptedFilePath));
            logger.LogInformation("File decrypted: {DecryptedFilePath}", decryptedFilePath);
        }

        foreach (var file in cweFiles)
        {
            File.Delete(file);
        }

        return decrypted;
    }
}
