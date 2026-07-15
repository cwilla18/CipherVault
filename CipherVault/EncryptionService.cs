using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using CipherVault.Core.Data;
using CipherVault.Core.Formats;
using CipherVault.Core.Helpers;
using CipherVault.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CipherVault;

internal class EncryptionService
{
    private readonly ICipher _cipher;
    private readonly IConsoleUi _ui;

    public EncryptionService(ICipher cipher, IConsoleUi ui)
    {
        _cipher = cipher;
        _ui = ui;
    }

    public void Start(ILogger logger)
    {
        try
        {
            _ui.Section("Select folder");
            var filePath = _ui.PromptExistingDirectory("Enter the path to the folder you want to encrypt:");

            _ui.Section("Set password");

            // Held only as a SecureString; converted to transient bytes inside
            // PasswordHelper.UsePasswordBytes and zeroed straight after derivation.
            using var secure = ReadValidPassword();

            _ui.Info("Password accepted. Preparing files...");

            var stopwatch = Stopwatch.StartNew();

            var salt = RandomNumberGenerator.GetBytes(Config.SaltSize);
            var associatedData = CweFileFormat.BuildAssociatedData(salt, Config.KdfIterations);

            var copiedFilePath = CreateFolderAndCopyContent(filePath, logger);

            if (string.IsNullOrEmpty(copiedFilePath))
            {
                _ui.Warn("No files found in that folder. Nothing to encrypt.");
                return;
            }

            // Derive the encryption key from the password bytes and random salt.
            var encryptionKey = PasswordHelper.UsePasswordBytes(secure,
                pw => _cipher.DeriveKeyFromPassword(pw, salt, Config.KdfIterations));

            _ui.Section("Encrypting");
            var encryptedCount = EncryptFiles(copiedFilePath, encryptionKey, salt, associatedData, logger);

            var zipPath = $"{copiedFilePath}.zip";
            FileHelper.CreateZip(copiedFilePath, logger);

            // Only remove the working folder once the archive is confirmed on disk.
            if (!File.Exists(zipPath))
                throw new IOException($"Archive was not created at {zipPath}; keeping working folder.");

            FileHelper.DeleteDirectory(copiedFilePath, logger);

            stopwatch.Stop();

            _ui.Summary("Encryption complete", new List<(string, string)>
            {
                ("Files encrypted", encryptedCount.ToString()),
                ("Output", zipPath),
                ("Elapsed", $"{stopwatch.Elapsed.TotalSeconds:F1}s"),
            });

            _ui.Success("Your files are encrypted. Keep your password safe - it cannot be recovered.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during encryption.");
            _ui.Error("Encryption failed. See the log for details.");
            throw;
        }
    }

    /// <summary>
    /// Prompts (masked, with confirmation) until the entered password satisfies
    /// the password policy, then returns it as a read-only <see cref="SecureString"/>.
    /// </summary>
    private SecureString ReadValidPassword()
    {
        _ui.Info("Password must be at least 10 characters and include an uppercase letter,");
        _ui.Info("a lowercase letter, a digit and a special character.");

        while (true)
        {
            SecureString secure = _ui.ReadSecretConfirmed(
                "Enter a password to encrypt the files:",
                "Confirm the password:");

            var isValid = PasswordHelper.UsePasswordChars(secure, chars => PasswordPolicy.IsValid(chars));
            if (isValid)
            {
                return secure;
            }

            secure.Dispose();
            _ui.Warn("That password does not meet the policy. Please try again.");
        }
    }

    private static string CreateFolderAndCopyContent(string sourceFilePath, ILogger logger)
    {
        var sourceName = $"{Path.GetFileName(sourceFilePath)}_encrypted";
        var destinationFolder = Path.Combine(sourceFilePath, sourceName);

        Directory.CreateDirectory(destinationFolder);

        // Exclude the output folder so a re-run does not ingest its own .cwe/zip.
        var directoryFiles = FileHelper.GetFilesFromDirectory(sourceFilePath)
            .Where(f => !f.StartsWith(destinationFolder, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (directoryFiles.Count == 0)
        {
            logger.LogInformation("No files found in the source directory.");
            return string.Empty;
        }

        foreach (var file in directoryFiles)
        {
            // Preserve the source tree so files in different subfolders can't collide.
            var relativePath = Path.GetRelativePath(sourceFilePath, file);
            var destinationFilePath = Path.Combine(destinationFolder, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);
            File.Copy(file, destinationFilePath, true);
        }

        return destinationFolder;
    }

    private int EncryptFiles(string filePath, byte[] encryptionKey, byte[] salt, byte[] associatedData, ILogger logger)
    {
        var files = FileHelper.GetFilesFromDirectory(filePath);

        if (files.Count == 0)
        {
            throw new FileNotFoundException($"No files found in the specified path: {filePath}");
        }

        var encrypted = 0;
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var attributes = File.GetAttributes(file);

            if (attributes.HasFlag(FileAttributes.Hidden))
            {
                continue; // Skip hidden files
            }

            var fileBytes = File.ReadAllBytes(file);

            // Encrypt the file content, binding the header as associated data.
            var encryptedPayload = _cipher.Encrypt(fileBytes, associatedData, encryptionKey);

            var outputPath = file + ".cwe";
            using (var outputStream = File.Create(outputPath))
            {
                CweFileFormat.Write(outputStream, salt, Config.KdfIterations, encryptedPayload);
            }

            // Verify the vault is recoverable before we ever delete a plaintext file.
            using (var verifyStream = File.OpenRead(outputPath))
            {
                var roundTrip = CweFileFormat.Read(verifyStream);
                var verifyAd = CweFileFormat.BuildAssociatedData(roundTrip.Salt, roundTrip.Iterations);
                var check = _cipher.Decrypt(roundTrip.Payload, verifyAd, encryptionKey);
                if (!check.SequenceEqual(fileBytes))
                    throw new CryptographicException(
                        $"Verification failed for {outputPath}; leaving original in place.");
            }

            encrypted++;
            _ui.Progress(i + 1, files.Count, Path.GetFileName(file));
            logger.LogInformation("File encrypted: {OutputPath}", outputPath);
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

        logger.LogInformation("All files encrypted under: {FilePath}", filePath);
        return encrypted;
    }
}
