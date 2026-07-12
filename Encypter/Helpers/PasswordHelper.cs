using Encypter.Data;
using Encypter.Records;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

namespace Encypter.Helpers;

internal static class PasswordHelper
{
    private static string _filePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.DpapiFileName);

    public static bool IsMasterPasswordSet(ILogger logger)
    {
        return File.Exists(_filePath);
    }

    public static SecureString ProcessPassewordInput(string inputMessage, ILogger logger)
    {
        try
        {
            var secureString = new SecureString();
            ConsoleKeyInfo keyInfo;
            var sanityCheck = new WhileLoopSanityCheck();

            Console.WriteLine(inputMessage);

            while (true)
            {
                keyInfo = Console.ReadKey(intercept: true);

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

                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    secureString.AppendChar(keyInfo.KeyChar);
                    Console.Write("*");
                }

                sanityCheck.ValidateAttempts();

            }
            secureString.MakeReadOnly();

            return secureString;
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while processing the password input: {ex.Message}");
            return null;
        }
    }

    public static string ConvertFromnSecureString(SecureString secureString, ILogger logger)
    {
        try
        {
            if (secureString == null)
            {
                throw new ArgumentNullException(nameof(secureString), "SecureString cannot be null.");
            }

            var unmanagedString = nint.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while converting SecureString to string: {ex.Message}");
            return null;
        }
    }

    public static void SaveMasterPassword(SecureString secureString, ILogger logger)
    {
        try
        {
            SaveToSecretManager(secureString);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error saving master password: {ex.Message}");
            throw;
        }
    }

    private static void SaveToSecretManager(SecureString secureString)
    {
        var bstrPtr = Marshal.SecureStringToBSTR(secureString);
        try
        {
            var length = Marshal.ReadInt32(bstrPtr - 4);
            var plainBytes = new byte[length];
            Marshal.Copy(bstrPtr, plainBytes, 0, length);

            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_filePath, encryptedBytes);
        }
        finally
        {
            Marshal.ZeroFreeBSTR(bstrPtr);
        }
    }
}
