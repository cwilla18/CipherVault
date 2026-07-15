using System;
using System.IO;
using CipherVault.Core.Interfaces; // IConsoleUi
using CipherVault.Core.UI;         // ConsoleUi
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Ui;

[TestClass]
public class ConsoleUiTests
{
    // Runs an action against a ConsoleUi with Console input/output redirected.
    // Only the line-based methods are exercised here; ReadSecret uses
    // Console.ReadKey, which cannot be driven by redirected input.
    private static T WithConsole<T>(string input, Func<IConsoleUi, T> action)
    {
        var originalIn = Console.In;
        var originalOut = Console.Out;
        try
        {
            Console.SetIn(new StringReader(input));
            Console.SetOut(new StringWriter());
            return action(new ConsoleUi());
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
    }

    // ---------- Confirm ----------

    [TestMethod]
    public void Confirm_EmptyInput_ReturnsDefaultTrue() => Assert.IsTrue(WithConsole("\n", ui => ui.Confirm("Proceed?", defaultYes: true)));

    [TestMethod]
    public void Confirm_EmptyInput_ReturnsDefaultFalse() => Assert.IsFalse(WithConsole("\n", ui => ui.Confirm("Proceed?", defaultYes: false)));

    [TestMethod]
    public void Confirm_Yes_ReturnsTrue() => Assert.IsTrue(WithConsole("y\n", ui => ui.Confirm("Proceed?", defaultYes: false)));

    [TestMethod]
    public void Confirm_No_ReturnsFalse() => Assert.IsFalse(WithConsole("n\n", ui => ui.Confirm("Proceed?", defaultYes: true)));

    // ---------- PromptExistingDirectory ----------

    [TestMethod]
    public void PromptExistingDirectory_RepromptsUntilValid()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cvui_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var input = "not-a-real-directory\n" + dir + "\n";
            var result = WithConsole(input, ui => ui.PromptExistingDirectory("Folder?"));
            Assert.AreEqual(dir, result);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void PromptExistingDirectory_StripsSurroundingQuotes()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cvui_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var input = "\"" + dir + "\"\n";
            var result = WithConsole(input, ui => ui.PromptExistingDirectory("Folder?"));
            Assert.AreEqual(dir, result);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // ---------- PromptExistingFile ----------

    [TestMethod]
    public void PromptExistingFile_EnforcesRequiredExtension()
    {
        var root = Path.Combine(Path.GetTempPath(), "cvui_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var txt = Path.Combine(root, "data.txt");
            var zip = Path.Combine(root, "data.zip");
            File.WriteAllText(txt, "x");
            File.WriteAllText(zip, "y");

            // First a real file with the wrong extension, then the .zip.
            var input = txt + "\n" + zip + "\n";
            var result = WithConsole(input, ui => ui.PromptExistingFile("File?", ".zip"));

            Assert.AreEqual(zip, result);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
