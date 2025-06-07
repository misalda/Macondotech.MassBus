using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CTM.EnterpriseBus.Common.Configuration;
using CTM.EnterpriseBus.Common.Services;
using CTM.EnterpriseBus.Unity.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;
using Unity;
using Unity.Injection;
using Unity.Lifetime;

namespace CTM.EventSubscriber.Unity.Webjob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            Assembly assembly = Assembly.GetEntryAssembly();

            var logConfig = new LoggerConfiguration()
           .MinimumLevel.Information()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
           .Enrich.WithProperty("Application", assembly.GetName().Name)
           .Enrich.WithProperty("Runtime", assembly.ImageRuntimeVersion)
           .Enrich.FromLogContext()
           .WriteTo.RollingFile(new JsonFormatter(), "Logs-{Date}.log", shared: true)
           .WriteTo.Console();

            Log.Logger =logConfig.CreateLogger();

            var container = new UnityContainer();
            var asbConfig = new BusConfiguration(azureServiceBusSettings:new AzureServiceBusSettings()
            {
                Uri = ConfigurationManager.AppSettings["AzureSbNameSpace"],
                KeyName = ConfigurationManager.AppSettings["AzureSbKeyName"],
                SharedAccessKey = ConfigurationManager.AppSettings["AzureSbSharedAccessKey"]
            });

            
            container.RegisterFactory<ILoggerFactory>((ctr, type, name) =>
            {
                return new LoggerFactory().AddSerilog(Log.Logger);
            },new SingletonLifetimeManager());

            var logger = new SerilogLoggerProvider().CreateLogger(nameof(Program));
            container.RegisterFactory(typeof(Microsoft.Extensions.Logging.ILogger<>),(ctr, type, name) =>
            {
                var loggerFactory = ctr.Resolve<ILoggerFactory>();
                var loggerType = type.GetGenericArguments()[0];
                var method = typeof(LoggerFactoryExtensions).GetMethod("CreateLogger",new[] {typeof(ILoggerFactory)});
                var genericMethod = method.MakeGenericMethod(loggerType);
                return genericMethod.Invoke(loggerFactory, new[] {loggerFactory});
            });
            container.AddCTMServiceBus(asbConfig, logger);

            var busControl = container.Resolve<ICTMEnterpriseBus>();

            var config = new JobHostConfiguration
            {
                JobActivator = new UnityJobActivator(container)
            };
            var host = new JobHost(config);

            //var builder = new HostBuilder();
            //builder.ConfigureWebJobs(b =>
            //{
            //    b.AddAzureStorageCoreServices();
            //});
            //var host = builder.Build();

            busControl.StartAsync(default(CancellationToken));

            host.RunAndBlock();

            busControl.StopAsync(default(CancellationToken));
        }
    }

    public class UnityJobActivator : IJobActivator
    {
        /// <summary>
        /// The unity container.
        /// </summary>
        private readonly IUnityContainer container;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityJobActivator"/> class.
        /// </summary>
        /// <param name="container">
        /// The unity container.
        /// </param>
        public UnityJobActivator(IUnityContainer container)
        {
            this.container = container;
        }

        /// <inheritdoc />
        public T CreateInstance<T>()
        {
            return this.container.Resolve<T>();
        }
    }
}
