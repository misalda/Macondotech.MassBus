using Azure.Storage.Blobs;
using MacondoTech.EnterpriseBus.Common.Configuration;
using MacondoTech.EnterpriseBus.Common.Infrastructure;
using MacondoTech.EnterpriseBus.Core.Extensions;
using MassTransit;
using Microsoft.Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Extensions.Logging;

namespace MacondoTech.Subscriber
{
    public class Startup
    {
        IConfigurationRoot Configuration { get; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddTransient(sp => new BlobServiceClient(Configuration["AzureStorageConnectionString"]));
            services.AddHttpClient();
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog();
            });
            services.AddTransient<IMessageDataRepository, BlobMessageDataRepository>();

            var p = new BusConfiguration(azureServiceBusSettings:new AzureServiceBusSettings()
            {
                Uri = Configuration["AzureSbNameSpace"],
                KeyName = Configuration["AzureSbKeyName"],
                SharedAccessKey = Configuration["AzureSbSharedAccessKey"]
            });
            var logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Startup));
            services.AddCTMServiceBus(p,logger);
        }
    }
}