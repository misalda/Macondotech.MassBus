using MacondoTech.EnterpriseBus.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MacondoTech.Subscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            // Startup.cs finally :)
            Startup startup = new Startup();
            startup.ConfigureServices(services);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var busService = serviceProvider.GetRequiredService<ICTMEnterpriseBus>();
            busService.StartAsync(new System.Threading.CancellationToken(false));

            Console.WriteLine("Waiting for messages");
            Console.ReadKey();

            busService.StopAsync(new System.Threading.CancellationToken(false));
        }
    }
}

