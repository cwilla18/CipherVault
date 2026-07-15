using System;
using CipherVault.Core;
using CipherVault.Core.Interfaces;
using CipherVault.Core.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CipherVault
{
    internal class Program
    {
        internal static int Main(string[] args)
        {
            using var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    // Keep the console for warnings/errors only; user-facing
                    // output goes through IConsoleUi, not the logger.
                    logging.SetMinimumLevel(LogLevel.Warning);
                })
                .ConfigureServices((context, services) =>
                {
                    // Register the cipher
                    services.AddTransient<ICipher, AesGcmCipher>();

                    // Register the console UI
                    services.AddSingleton<IConsoleUi, ConsoleUi>();

                    // Register EncryptionService as itself
                    services.AddTransient<EncryptionService>();
                }).Build();

            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
            IConsoleUi ui = host.Services.GetRequiredService<IConsoleUi>();

            Console.CancelKeyPress += (_, e) =>
            {
                // Originals survive an interrupt (encryption verifies each file
                // before deleting any plaintext), but a working folder may remain.
                ui.Warn("Cancelled. A partial working folder may remain; no originals were lost.");
            };

            ui.Banner("CipherVault  -  Encrypt", "Securely encrypt a folder into a password-protected vault.");

            try
            {
                EncryptionService encryptionService = host.Services.GetRequiredService<EncryptionService>();
                encryptionService.Start(logger);
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Encryption terminated with an error.");
                return 1;
            }
        }
    }
}
