using Encypter.Data;
using Encypter.Records;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Encypter
{
    internal class Validate
    {
        public Validate() { }

        public bool ValidatePassword(string password)
        {
            try
            {
                var sanityCheck = new WhileLoopSanityCheck();

                while (string.IsNullOrEmpty(password) || !PasswordIsValid(password))
                {
                    Console.WriteLine("Invalid  Password. Please enter a valid Master Password.");
                    password = Console.ReadLine();

                    sanityCheck.ValidateAttempts();
                }

                return true;
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred while validating the password.", e); 
            }

        }

        public bool PasswordIsValid(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 10)
            {
                return false;
            }

            bool hasUpperCase = false;
            bool hasLowerCase = false;
            bool hasDigit = false;
            bool hasSpecialChar = false;
            
            foreach (char c in password)
            {
                if (char.IsUpper(c))
                {
                    hasUpperCase = true;
                }
                else if (char.IsLower(c)) 
                { 
                    hasLowerCase = true; 
                }
                else if (char.IsDigit(c)) 
                { 
                    hasDigit = true; 
                }
                else if (!char.IsLetterOrDigit(c)) 
                { 
                    hasSpecialChar = true; 
                }

                // If all conditions are met, we can exit early
                if (hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
