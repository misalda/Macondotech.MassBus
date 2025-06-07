using CTM.EnterpriseBus.Common.Configuration;
using CTM.EnterpriseBus.Common.Infrastructure;
using CTM.EnterpriseBus.Core.Extensions;
using MassTransit.MessageData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Serilog;
using Serilog.Extensions.Logging;

namespace CTM.Producer
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }
        public void ConfigureServices(IServiceCollection services)
        {

            var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            services.AddSingleton(storageAccount);
            services.AddTransient<IMessageDataRepository, BlobMessageDataRepository>();

            var p = new BusConfiguration(azureServiceBusSettings:new AzureServiceBusSettings()
            {
                Uri = Configuration["AzureSbNameSpace"],
                KeyName = Configuration["AzureSbKeyName"],
                SharedAccessKey = Configuration["AzureSbSharedAccessKey"]
            });

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog();
            });
            var logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Startup));
            services.AddCTMServiceBus(p,logger);
            
        }
    }
}
