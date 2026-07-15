using System;
using CipherVault.Core;
using CipherVault.Core.Interfaces;
using CipherVault.Core.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Decrypter
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
                    services.AddTransient<ICipher, AesGcmCipher>();
                    services.AddSingleton<IConsoleUi, ConsoleUi>();
                    services.AddTransient<DecryptionService>();
                }).Build();

            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
            IConsoleUi ui = host.Services.GetRequiredService<IConsoleUi>();

            Console.CancelKeyPress += (_, e) =>
            {
                ui.Warn("Cancelled. Some files may be only partially restored; re-run to finish.");
            };

            ui.Banner("CipherVault  -  Decrypt", "Restore files from a password-protected vault.");

            try
            {
                DecryptionService decryptionService = host.Services.GetRequiredService<DecryptionService>();
                decryptionService.DecryptFile(logger);
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Decryption terminated with an error.");
                return 1;
            }
        }
    }
}
