using System;
using System.Runtime.InteropServices;
using System.Security;
using CipherVault.Core.Records;
using Microsoft.Extensions.Logging;

namespace CipherVault.Core.Helpers;

public static class PasswordHelper
{
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
            logger.LogError($"An error occurred while processing the password input: {ex}");
            throw;
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
                return Marshal.PtrToStringUni(unmanagedString)
                    ?? throw new InvalidOperationException("Failed to read the SecureString contents.");
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while converting SecureString to string: {ex}");
            throw;
        }
    }
}
