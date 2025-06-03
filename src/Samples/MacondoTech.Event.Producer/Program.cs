using MacondoTech.Producer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using CommandDotNet;
using CommandDotNet.IoC.MicrosoftDependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Reflection;
using MacondoTech.EnterpriseBus.Common.Services;

namespace MacondoTech.EventProducer
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Assembly assembly = Assembly.GetEntryAssembly();

           Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
           .Enrich.WithProperty("Application", assembly.GetName().Name)
           .Enrich.WithProperty("Runtime", assembly.ImageRuntimeVersion)
           .Enrich.FromLogContext()
           .WriteTo.RollingFile(new JsonFormatter(), "Logs-{Date}.log", shared: true)
           .WriteTo.Console()
           .CreateLogger();

            IServiceCollection services = new ServiceCollection();

            Startup startup = new Startup();
            startup.ConfigureServices(services);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var busService = serviceProvider.GetRequiredService<ICTMEnterpriseBus>();
            await busService.StartAsync(new System.Threading.CancellationToken(false));

            AppRunner<EventProducer> appRunner = new AppRunner<EventProducer>().UseMicrosoftDependencyInjection(serviceProvider);
            return Task.FromResult(appRunner.Run(args)).GetAwaiter().GetResult();
        }
    }
}
