using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using CipherVault.Core.Data;
using CipherVault.Core.Interfaces;

namespace CipherVault.Core.UI;

/// <summary>
/// Dependency-free console UI built on <see cref="Console"/>. Adds colour,
/// symbols, validated prompts, masked secret entry and progress/summary
/// rendering without pulling in any third-party framework.
/// </summary>
public sealed class ConsoleUi : IConsoleUi
{
    private const int Width = 64;

    public void Banner(string title, string? subtitle = null)
    {
        var line = new string('=', Width);
        WriteColour(line, ConsoleColor.Cyan);
        WriteColour("  " + title, ConsoleColor.White);
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            WriteColour("  " + subtitle, ConsoleColor.DarkGray);
        }
        WriteColour(line, ConsoleColor.Cyan);
        Console.WriteLine();
    }

    public void Section(string title)
    {
        Console.WriteLine();
        var dashes = Math.Max(0, Width - title.Length - 3);
        WriteColour($"-- {title} {new string('-', dashes)}", ConsoleColor.DarkCyan);
    }

    public void Info(string message) => WriteColour($"  {message}", ConsoleColor.Gray);
    public void Success(string message) => WriteColour($"  [OK]   {message}", ConsoleColor.Green);
    public void Warn(string message) => WriteColour($"  [WARN] {message}", ConsoleColor.Yellow);
    public void Error(string message) => WriteColour($"  [FAIL] {message}", ConsoleColor.Red);

    public string Prompt(string message)
    {
        WriteColour($"  {message}", ConsoleColor.White);
        Console.Write("  > ");
        return Console.ReadLine() ?? string.Empty;
    }

    public bool Confirm(string message, bool defaultYes = true)
    {
        var hint = defaultYes ? "[Y/n]" : "[y/N]";
        WriteColour($"  {message} {hint}", ConsoleColor.White);
        Console.Write("  > ");
        var answer = (Console.ReadLine() ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(answer))
        {
            return defaultYes;
        }

        return answer.StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }

    public string PromptExistingDirectory(string message)
    {
        var attempts = 0;
        WriteColour($"  {message}", ConsoleColor.White);

        while (true)
        {
            Console.Write("  > ");
            var input = (Console.ReadLine() ?? string.Empty).Trim().Trim('"');

            if (string.IsNullOrEmpty(input))
            {
                Warn("Path cannot be empty. Please try again.");
            }
            else if (!Directory.Exists(input))
            {
                Warn("That folder does not exist. Please check the path and try again.");
            }
            else
            {
                return input;
            }

            GuardAttempts(ref attempts);
        }
    }

    public string PromptExistingFile(string message, string? requiredExtension = null)
    {
        var attempts = 0;
        WriteColour($"  {message}", ConsoleColor.White);

        while (true)
        {
            Console.Write("  > ");
            var input = (Console.ReadLine() ?? string.Empty).Trim().Trim('"');

            if (string.IsNullOrEmpty(input))
            {
                Warn("Path cannot be empty. Please try again.");
            }
            else if (!File.Exists(input))
            {
                Warn("That file does not exist. Please check the path and try again.");
            }
            else if (requiredExtension is not null &&
                     !input.EndsWith(requiredExtension, StringComparison.OrdinalIgnoreCase))
            {
                Warn($"Expected a {requiredExtension} file. Please try again.");
            }
            else
            {
                return input;
            }

            GuardAttempts(ref attempts);
        }
    }

    public SecureString ReadSecret(string message)
    {
        WriteColour($"  {message}", ConsoleColor.White);
        Console.Write("  > ");
        return ReadMasked();
    }

    public SecureString ReadSecretConfirmed(string message, string confirmMessage)
    {
        var attempts = 0;

        while (true)
        {
            var first = ReadSecret(message);
            var second = ReadSecret(confirmMessage);

            if (SecretsMatch(first, second))
            {
                second.Dispose();
                first.MakeReadOnly();
                return first;
            }

            first.Dispose();
            second.Dispose();
            Warn("Passwords did not match. Please try again.");
            GuardAttempts(ref attempts);
        }
    }

    public void Progress(int current, int total, string label)
    {
        var counter = $"[{current}/{total}]";
        // Pad to clear any longer previous line, then return to start with \r.
        var text = $"  {counter} {label}";
        if (text.Length < Width)
        {
            text += new string(' ', Width - text.Length);
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("\r" + text);
        Console.ResetColor();

        if (current >= total)
        {
            Console.WriteLine();
        }
    }

    public void Summary(string title, IReadOnlyList<(string Label, string Value)> rows)
    {
        Console.WriteLine();
        var labelWidth = rows.Count == 0 ? 0 : rows.Max(r => r.Label.Length);
        var top = "+" + new string('-', Width - 2) + "+";

        WriteColour(top, ConsoleColor.Cyan);
        WriteColour("| " + title.PadRight(Width - 4) + " |", ConsoleColor.White);
        WriteColour("+" + new string('-', Width - 2) + "+", ConsoleColor.Cyan);

        foreach (var (label, value) in rows)
        {
            var line = $"{label.PadRight(labelWidth)}  {value}";
            if (line.Length > Width - 4)
            {
                line = line.Substring(0, Width - 4);
            }

            WriteColour("| " + line.PadRight(Width - 4) + " |", ConsoleColor.Gray);
        }

        WriteColour(top, ConsoleColor.Cyan);
        Console.WriteLine();
    }

    private static SecureString ReadMasked()
    {
        var secureString = new SecureString();

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (secureString.Length > 0)
                {
                    secureString.RemoveAt(secureString.Length - 1);
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(keyInfo.KeyChar) && secureString.Length < Config.MaxPasswordLength)
            {
                secureString.AppendChar(keyInfo.KeyChar);
                Console.Write("*");
            }
        }

        Console.WriteLine();
        return secureString;
    }

    // Bounds re-prompt loops so redirected/EOF input can't spin forever.
    private static void GuardAttempts(ref int attempts)
    {
        if (++attempts >= Config.MaxPromptAttempts)
        {
            throw new InvalidOperationException("Too many invalid attempts.");
        }
    }

    private static bool SecretsMatch(SecureString a, SecureString b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var ptrA = nint.Zero;
        var ptrB = nint.Zero;
        try
        {
            ptrA = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(a);
            ptrB = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(b);

            for (var i = 0; i < a.Length; i++)
            {
                var charA = System.Runtime.InteropServices.Marshal.ReadInt16(ptrA, i * 2);
                var charB = System.Runtime.InteropServices.Marshal.ReadInt16(ptrB, i * 2);
                if (charA != charB)
                {
                    return false;
                }
            }

            return true;
        }
        finally
        {
            if (ptrA != nint.Zero)
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptrA);
            }
            if (ptrB != nint.Zero)
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptrB);
            }
        }
    }

    private static void WriteColour(string message, ConsoleColor colour)
    {
        Console.ForegroundColor = colour;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
