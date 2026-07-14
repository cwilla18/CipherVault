using CipherVault.Core;
using CipherVault.Core.Interfaces;
using CipherVault.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CipherVault
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureServices((context, services) =>
                {
                    // Register Validate as IValidate (separate validation service)
                    services.AddTransient<IValidate, Validate>();

                    // Register the cipher
                    services.AddTransient<ICipher, AesGcmCipher>();

                    // Register EncryptionService as itself
                    services.AddTransient<EncryptionService>();
                }).Build();


            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

            EncryptionService encryptionService = host.Services.GetRequiredService<EncryptionService>();

            encryptionService.Start(logger);
        }
    }
}
