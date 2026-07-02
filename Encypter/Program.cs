using Encypter.Data;
using System;
using System.Threading;

namespace Encypter
{
    internal class Program
    {

        internal static void Main(string[] args)
        {
            var cancellation = new CancellationTokenSource();

            Console.WriteLine("===== Encryter App Started =====");

            var startUp = new StartUp();
            startUp.Initialise();

            var master = Environment.GetEnvironmentVariable(Config.MasterPasswordEnvVar, EnvironmentVariableTarget.User);
            var process = new Process();

            while (!cancellation.Token.IsCancellationRequested)
            {
                process.Start(master);

                Console.WriteLine("Enter 'exit' to close the application.");
                var input = Console.ReadLine();

                if (input?.ToLower() == "exit")
                {
                    cancellation.Cancel();
                }
            }

            Console.WriteLine("Thank you, Press any key to exit...");
            Console.ReadKey();
        }
    }
}