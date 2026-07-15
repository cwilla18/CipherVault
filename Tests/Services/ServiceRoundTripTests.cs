using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using CipherVault;                 // EncryptionService
using CipherVault.Core;            // AesGcmCipher
using CipherVault.Core.Interfaces; // ICipher
using CipherVault.Core.UI;         // IConsoleUi
using Decrypter;                   // DecryptionService
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Services;

[TestClass]
public class ServiceRoundTripTests
{
    private const string GoodPassword = "Str0ng!Pass99";

    private string _root = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _root = Path.Combine(Path.GetTempPath(), "cvsvc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [TestMethod]
    public void EncryptThenDecrypt_RestoresOriginalFiles()
    {
        var source = Path.Combine(_root, "vault");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "a.txt"), "hello world");
        var binary = new byte[512];
        RandomNumberGenerator.Fill(binary);
        File.WriteAllBytes(Path.Combine(source, "b.bin"), binary);

        // --- Encrypt ---
        var encUi = new FakeConsoleUi { Directory = source, Password = GoodPassword };
        var encryptService = new EncryptionService(new AesGcmCipher(), encUi);
        encryptService.Start(NullLogger.Instance);

        var zip = Path.Combine(source, "vault_encrypted.zip");
        Assert.IsTrue(File.Exists(zip), "encryption should produce the vault zip");

        // --- Decrypt ---
        var decUi = new FakeConsoleUi { FilePath = zip, Password = GoodPassword };
        var decryptService = new DecryptionService(new AesGcmCipher(), decUi);
        decryptService.DecryptFile(NullLogger.Instance);

        var decryptedDir = Path.Combine(source, "vault_decrypted");
        var restored = Directory.GetFiles(decryptedDir, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".cwe", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(Path.GetFileName);

        Assert.IsTrue(restored.ContainsKey("a.txt"), "a.txt should be restored");
        Assert.IsTrue(restored.ContainsKey("b.bin"), "b.bin should be restored");
        Assert.AreEqual("hello world", File.ReadAllText(restored["a.txt"]));
        CollectionAssert.AreEqual(binary, File.ReadAllBytes(restored["b.bin"]));
    }

    [TestMethod]
    public void Encrypt_EmptyFolder_WarnsAndProducesNoZip()
    {
        var source = Path.Combine(_root, "empty");
        Directory.CreateDirectory(source);

        var encUi = new FakeConsoleUi { Directory = source, Password = GoodPassword };
        var encryptService = new EncryptionService(new AesGcmCipher(), encUi);
        encryptService.Start(NullLogger.Instance);

        Assert.IsFalse(File.Exists(Path.Combine(source, "empty_encrypted.zip")),
            "no zip should be produced for an empty folder");
        Assert.IsTrue(encUi.Warnings.Count > 0, "the user should be warned there is nothing to encrypt");
    }

    [TestMethod]
    public void Decrypt_WrongPassword_Throws()
    {
        var source = Path.Combine(_root, "vault");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "a.txt"), "hello world");

        var encUi = new FakeConsoleUi { Directory = source, Password = GoodPassword };
        new EncryptionService(new AesGcmCipher(), encUi).Start(NullLogger.Instance);
        var zip = Path.Combine(source, "vault_encrypted.zip");

        var decUi = new FakeConsoleUi { FilePath = zip, Password = "Wr0ng!Password" };
        var decryptService = new DecryptionService(new AesGcmCipher(), decUi);

        var threw = false;
        try
        {
            decryptService.DecryptFile(NullLogger.Instance);
        }
        catch
        {
            threw = true;
        }

        Assert.IsTrue(threw, "decrypting with the wrong password should throw");
    }
}

/// <summary>
/// Scripted <see cref="IConsoleUi"/> for driving the services without a real
/// console. Returns canned answers and records warnings/errors.
/// </summary>
internal sealed class FakeConsoleUi : IConsoleUi
{
    public string? Directory { get; set; }
    public string? FilePath { get; set; }
    public string Password { get; set; } = string.Empty;
    public bool ConfirmResult { get; set; } = true;
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();

    public void Banner(string title, string? subtitle = null) { }
    public void Section(string title) { }
    public void Info(string message) { }
    public void Success(string message) { }
    public void Warn(string message) => Warnings.Add(message);
    public void Error(string message) => Errors.Add(message);
    public string Prompt(string message) => string.Empty;
    public bool Confirm(string message, bool defaultYes = true) => ConfirmResult;
    public string PromptExistingDirectory(string message) => Directory!;
    public string PromptExistingFile(string message, string? requiredExtension = null) => FilePath!;
    public SecureString ReadSecret(string message) => ToSecure(Password);
    public SecureString ReadSecretConfirmed(string message, string confirmMessage) => ToSecure(Password);
    public void Progress(int current, int total, string label) { }
    public void Summary(string title, IReadOnlyList<(string Label, string Value)> rows) { }

    private static SecureString ToSecure(string value)
    {
        var secure = new SecureString();
        foreach (var c in value)
        {
            secure.AppendChar(c);
        }
        secure.MakeReadOnly();
        return secure;
    }
}
