using Encypter.Data;
using System;
using System.IO;

namespace Encypter
{
    internal class Validate
    {
        public Validate() { }

        public string SetPassword(string password)
        {

            var attempts = 0;

            while (string.IsNullOrEmpty(password) || !PasswordIsValid(password))
            {
                Console.WriteLine("Invalid  Password. Please enter a valid Master Password.");
                password = Console.ReadLine();
                attempts++;
                        
                if (attempts >= Config.WhileLoopSanityCheck)
                {
                    throw new InvalidOperationException("Too many invalid attempts.");
                }
            }

            return password;
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

        public bool ValidateFileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("File path cannot be empty.");
                return false;
            }
            if (!Directory.Exists(filePath))
            {
                Console.WriteLine("The specified directory does not exist.");
                return false;
            }
            return true;
        }
    }
}
