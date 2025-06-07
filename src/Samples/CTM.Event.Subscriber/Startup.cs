using CTM.EnterpriseBus.Common.Configuration;
using CTM.EnterpriseBus.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CTM.Subscriber
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
            var p = new BusConfiguration(azureServiceBusSettings:new AzureServiceBusSettings()
                {
                    Uri = Configuration["AzureSbNameSpace"],
                    KeyName = Configuration["AzureSbKeyName"],
                    SharedAccessKey = Configuration["AzureSbSharedAccessKey"]
                }
            );
            services.AddCTMServiceBus(p);
        }
    }
}