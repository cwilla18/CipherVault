using Encypter.Data;
using System;

namespace Encypter
{
    internal class StartUp
    {

        public void Initialise()
        {
            try
            {
                var environmentVar = Environment.GetEnvironmentVariable(Config.MasterPasswordEnvVar, EnvironmentVariableTarget.User);

                if (!string.IsNullOrEmpty(environmentVar))
                {
                    return;
                }

                var pass = SetMasterPassword();
                SaveMasterPassword(pass);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in start up: {ex.Message}");
                throw;
            }
        }

        private string SetMasterPassword()
        {
            var validate = new Validate();

            Console.WriteLine("Please enter a Master Password. \n This must be 10 Charaters Long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");
            var masterPassword = Console.ReadLine();

            validate.SetPassword(masterPassword);


            return masterPassword;
        }

        private void SaveMasterPassword(string masterPassword)
        {
            try
            {
                Environment.SetEnvironmentVariable(Config.MasterPasswordEnvVar, masterPassword, EnvironmentVariableTarget.User);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting master password: {ex.Message}");
                throw;
            }
        }
    }
}
