using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encypter
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
                    services.AddSingleton<StartUp>();
                    services.AddSingleton<EncryptionService>();
                    services.AddSingleton<Validate>();
                    //TODO: add IEncryptor, AesGcmEncryptor
                }).Build();

            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

            Validate? validate = host.Services.GetRequiredService<Validate>();
            StartUp? startup = host.Services.GetRequiredService<StartUp>();
            EncryptionService? encryptionService = host.Services.GetRequiredService<EncryptionService>();

            startup.Initialise(logger, validate);
            encryptionService.Start(logger, validate);
        }
    }
}
