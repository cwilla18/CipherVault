using Encypter.Data;
using Encypter.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Security;

namespace Encypter
{
    internal class StartUp
    {

        public void Initialise(ILogger logger, Validate? validate)
        {
            try
            {
                if (PasswordHelper.IsMasterPasswordSet(logger))
                {
                    logger.LogInformation("Master Password is already set.");
                    return;
                }

                var secureMasterPassword = PasswordHelper.ProcessPassewordInput("Please enter a Master Password. \n This must be 10 Charaters Long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.", logger);
                var masterPassword = PasswordHelper.ConvertFromnSecureString(secureMasterPassword, logger);

                if(validate.ValidatePassword(masterPassword))
                {
                    //Destroy the insecure string after use
                    masterPassword = string.Empty;

                    PasswordHelper.SaveMasterPassword(secureMasterPassword, logger);

                    //After saving the password, dispose of the SecureString to free resources
                    secureMasterPassword.Dispose();
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Issue Initialising start up: {e.Message}");
                throw;
            }
        }
    }
}
