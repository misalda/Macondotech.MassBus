using CTM.EnterpriseBus.Common.Configuration;
using CTM.EnterpriseBus.Common.Infrastructure;
using CTM.EnterpriseBus.Common.Services;
using CTM.EnterpriseBus.Core.Extensions;
using MassTransit.MessageData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace CTM.Event.Subscriber.WebJob
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

            var host = new HostBuilder()
                .UseEnvironment("Development")
                .ConfigureWebJobs(b =>
                {
                    b.AddAzureStorageCoreServices()
                    .AddAzureStorage()
                    .AddServiceBus();
                })
                .ConfigureAppConfiguration(b =>
                {
                    // Adding command line as a configuration source
                    b.AddJsonFile("appsettings.json");
                })
                .ConfigureServices((context, services) =>
                {
                    var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
                    services.AddSingleton(storageAccount);

                    var serviceCollection = services.BuildServiceProvider();
                    var blobMessageDataRepositoryLogger = serviceCollection.GetRequiredService<ILogger<BlobMessageDataRepository>>();

                    var blobMessageRepository = new BlobMessageDataRepository(
                        storageAccount,
                        blobMessageDataRepositoryLogger
                    );
                    services.AddTransient<IMessageDataRepository>(sp => blobMessageRepository);

                    var asbConfig = new BusConfiguration(azureServiceBusSettings: new AzureServiceBusSettings()
                    {
                        Uri = context.Configuration["AzureSbNameSpace"],
                        KeyName = context.Configuration["AzureSbKeyName"],
                        SharedAccessKey = context.Configuration["AzureSbSharedAccessKey"]
                    });

                    var logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));

                    services.AddCTMServiceBus(asbConfig, logger, blobMessageRepository);
                    services.AddHostedService<CTMEnterpriseBusService>();
                })
                .UseSerilog()
                .Build();

            await host.RunAsync();
        }
    }
}
