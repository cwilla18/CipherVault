using System;
using CipherVault.Core.Helpers;
using CipherVault.Core.Records;

namespace CipherVault.Interfaces;

public interface IValidate
{
    public bool ValidatePassword(string password)
    {
        try
        {
            var sanityCheck = new WhileLoopSanityCheck();

            while (string.IsNullOrEmpty(password) || !PasswordPolicy.IsValid(password))
            {
                Console.WriteLine("Invalid  Password. Please enter a valid Master Password.");
                password = Console.ReadLine() ?? string.Empty;

                sanityCheck.ValidateAttempts();
            }

            return true;
        }
        catch (Exception e)
        {
            throw new Exception("An error occurred while validating the password.", e);
        }
    }
}
