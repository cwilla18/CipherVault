using System;
using System.IO;
using System.Linq;
using CipherVault.Core.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Core;

[TestClass]
public class FileHelperTests
{
    private string _root = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _root = Path.Combine(Path.GetTempPath(), "cvtests_" + Guid.NewGuid().ToString("N"));
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

    // ---------- validation ----------

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ValidateFileExists_NullOrEmpty_ReturnsFalse(string? path)
    {
        Assert.IsFalse(FileHelper.ValidateFileExists(path, NullLogger.Instance));
    }

    [TestMethod]
    public void ValidateFileExists_NonexistentDirectory_ReturnsFalse()
    {
        var missing = Path.Combine(_root, "does-not-exist");
        Assert.IsFalse(FileHelper.ValidateFileExists(missing, NullLogger.Instance));
    }

    [TestMethod]
    public void ValidateFileExists_ExistingDirectory_ReturnsTrue()
    {
        Assert.IsTrue(FileHelper.ValidateFileExists(_root, NullLogger.Instance));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ValidateZipFileExists_NullOrEmpty_ReturnsFalse(string? path)
    {
        Assert.IsFalse(FileHelper.ValidateZipFileExists(path, NullLogger.Instance));
    }

    [TestMethod]
    public void ValidateZipFileExists_ExistingFile_ReturnsTrue()
    {
        var file = Path.Combine(_root, "archive.zip");
        File.WriteAllText(file, "not really a zip");
        Assert.IsTrue(FileHelper.ValidateZipFileExists(file, NullLogger.Instance));
    }

    // ---------- enumeration ----------

    [TestMethod]
    public void GetFilesFromDirectory_ReturnsFilesRecursively()
    {
        File.WriteAllText(Path.Combine(_root, "top.txt"), "a");
        var sub = Path.Combine(_root, "nested");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "deep.txt"), "b");

        var files = FileHelper.GetFilesFromDirectory(_root);

        Assert.AreEqual(2, files.Count);
        CollectionAssert.AreEquivalent(
            new[] { "top.txt", "deep.txt" },
            files.Select(Path.GetFileName).ToArray());
    }

    // ---------- zip round trip ----------

    [TestMethod]
    public void CreateZip_Then_Extract_RoundTripsContents()
    {
        var content = Path.Combine(_root, "content");
        Directory.CreateDirectory(content);
        File.WriteAllText(Path.Combine(content, "one.txt"), "first");
        File.WriteAllText(Path.Combine(content, "two.txt"), "second");

        FileHelper.CreateZip(content, NullLogger.Instance);
        var zip = content + ".zip";
        Assert.IsTrue(File.Exists(zip), "the zip should have been created");

        var extract = Path.Combine(_root, "extract");
        FileHelper.ExtractZip(zip, extract, NullLogger.Instance);

        var extracted = Directory.GetFiles(extract, "*", SearchOption.AllDirectories).ToDictionary(Path.GetFileName);
        Assert.AreEqual("first", File.ReadAllText(extracted["one.txt"]));
        Assert.AreEqual("second", File.ReadAllText(extracted["two.txt"]));
    }

    [TestMethod]
    public void CreateZip_PreservesSubfolderStructure()
    {
        var content = Path.Combine(_root, "content");
        var nested = Path.Combine(content, "sub");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(content, "top.txt"), "top");
        File.WriteAllText(Path.Combine(nested, "deep.txt"), "deep");

        FileHelper.CreateZip(content, NullLogger.Instance);
        var extract = Path.Combine(_root, "extract");
        FileHelper.ExtractZip(content + ".zip", extract, NullLogger.Instance);

        Assert.AreEqual("deep", File.ReadAllText(Path.Combine(extract, "sub", "deep.txt")));
    }

    // ---------- delete ----------

    [TestMethod]
    public void DeleteDirectory_RemovesExistingDirectory()
    {
        var dir = Path.Combine(_root, "to-delete");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "f.txt"), "x");

        FileHelper.DeleteDirectory(dir, NullLogger.Instance);

        Assert.IsFalse(Directory.Exists(dir));
    }

    [TestMethod]
    public void DeleteDirectory_NonexistentPath_DoesNotThrow()
    {
        var missing = Path.Combine(_root, "never-existed");

        // Should be a no-op, not an exception.
        FileHelper.DeleteDirectory(missing, NullLogger.Instance);

        Assert.IsFalse(Directory.Exists(missing));
    }
}
