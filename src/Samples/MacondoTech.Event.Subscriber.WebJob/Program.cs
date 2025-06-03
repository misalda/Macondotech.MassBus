using MacondoTech.EnterpriseBus.Common.Configuration;
using MacondoTech.EnterpriseBus.Common.Infrastructure;
using MacondoTech.EnterpriseBus.Common.Services;
using MacondoTech.EnterpriseBus.Core.Extensions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System;

namespace MacondoTech.Event.Subscriber.WebJob
{
    class Program
    {
        static async Task Main(string[] args)
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

            var builder = new HostBuilder();

            builder.ConfigureWebJobs(b =>
            {
                b.AddAzureStorageBlobs();
            })
            .ConfigureAppConfiguration(b =>
            {
                // Adding command line as a configuration source
                b.AddJsonFile("appsettings.json");
            });
            builder.ConfigureServices((context, services) =>
            {
                var storageAccount = context.Configuration["StorageAccount"];
                services.AddSingleton(storageAccount);
                services.AddHttpClient();

                //var serviceCollection = services.BuildServiceProvider();

                services.AddTransient<IMessageDataRepository>();

                var asbConfig = new BusConfiguration(azureServiceBusSettings: new AzureServiceBusSettings()
                {
                    Uri = context.Configuration["AzureSbNameSpace"],
                    KeyName = context.Configuration["AzureSbKeyName"],
                    SharedAccessKey = context.Configuration["AzureSbSharedAccessKey"]
                });

                var logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));

                services.AddCTMServiceBus(asbConfig, logger);
                services.AddHostedService<CTMEnterpriseBusService>();
            });

            var host = builder.Build();
            await host.RunAsync();               
        }
    }
}
