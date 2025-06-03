using Azure;
using MacondoTech.EnterpriseBus.Common.Configuration;
using MacondoTech.EnterpriseBus.Common.Services;
using MacondoTech.EnterpriseBus.Conventions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MacondoTech.EnterpriseBus.Core.Extensions
{
    public static class CTMServiceBusRegistrationExtensions
    {
        public static void AddCTMServiceBus(this IServiceCollection services, Action<IBusRegistrationConfigurator> configure)
        {
            services.AddMassTransit(configure);
        }
        public static void AddCTMServiceBus(this IServiceCollection services, BusConfiguration busConfiguration, ILogger logger = null)
        {
            services.AddMassTransit(x =>
            {
                var map = busConfiguration.TopologyMap;
                foreach (var consumer in map.EventConsumers.SelectMany(i => i.ConsumerClassTypes))
                {
                    x.AddConsumer(consumer);
                    logger?.LogInformation("Adding event consumer {ConsumerName}", consumer.FullName);
                }

                foreach (var consumer in map.MessageConsumers.SelectMany(i => i.ConsumerClassTypes))
                {
                    x.AddConsumer(consumer);
                    logger?.LogInformation("Adding message consumer '{ConsumerName}'", consumer.FullName);
                }

                foreach (var consumer in map.RequestConsumers.SelectMany(i => i.ConsumerClassTypes))
                {
                    x.AddConsumer(consumer);
                    logger?.LogInformation("Adding request consumer '{ConsumerName}'", consumer.FullName);
                }

                x.UsingAzureServiceBus((context, cfg) =>
                {
                    cfg.RequiresSession = false;
                    cfg.ConcurrentMessageLimit = 500;
                    cfg.Host(busConfiguration.AzureServiceBus.ConnectionString);

                    logger?.LogInformation("Connected to azure service bus namespace '{namespace}'", busConfiguration.AzureServiceBus.Uri);

                    var messageRepository = context.GetRequiredService<IMessageDataRepository>();
                    if (messageRepository != null)
                    {
                        cfg.UseMessageData(messageRepository);
                        logger?.LogInformation("Using message repository '{messageRepository}'", messageRepository.GetType().Name);
                    }

                    SetupSubscriptionEndpoints(map.DefaultEndPoint, context, cfg, map.EventConsumers, logger, messageRepository);

                    SetupQueueEndpoints(context, cfg, map.MessageConsumers, logger, messageRepository);

                    SetupQueueEndpoints(context, cfg, map.RequestConsumers, logger, messageRepository);
                });

                var methodInfo = typeof(IRegistrationConfigurator).GetMethod("AddRequestClient", new[] { typeof(Uri), typeof(RequestTimeout) });
                foreach (var endpoint in map.RequestClientEndpoints)
                {
                    MethodInfo genericMethod = methodInfo.MakeGenericMethod(endpoint.Key);
                    genericMethod.Invoke(x, new object[] { new Uri($"{busConfiguration.AzureServiceBus.Uri}/{endpoint.Value}"), RequestTimeout.After(m: 10) });
                }
            });
            services.AddSingleton(busConfiguration);
            services.AddSingleton<ICTMEnterpriseBus, CTMEnterpriseBusService>();
        }
        private static void SetupQueueEndpoints(IRegistrationContext context, IServiceBusBusFactoryConfigurator cfg, IEnumerable<ConsumerEntry> consumerEntries, ILogger logger, IMessageDataRepository messageRepository)
        {

            foreach (var entry in consumerEntries)
            {
                if (!entry.ConsumerClassTypes.Any())
                    continue;

                logger?.LogInformation("Configuring queue '{queueName}' to handle contract '{contractName}'", entry.ReceiveEndPoint, entry.ContractClassType);


                cfg.ReceiveEndpoint(entry.ReceiveEndPoint, c =>
                {
                    c.EnableDeadLetteringOnMessageExpiration = true;
                    c.MaxDeliveryCount = 3;
                    c.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1)));


                    foreach (Type cs in entry.ConsumerClassTypes)
                    {
                        c.ConfigureConsumer(context, cs);
                        logger?.LogInformation("Adding consumer '{consumerName}' to queue '{queueName}' to handle contract '{contractName}'", cs.Name, entry.ReceiveEndPoint, entry.ContractClassType.Name);
                    }
                });
            }
        }

        private static void ConfigureMessageRepository(this IConsumePipeConfigurator c, IMessageDataRepository messageRepository, ConsumerEntry entry, ILogger logger)
        {
            var hasUseStorageAttribute = entry.ContractClassType.GetCustomAttributes(typeof(UseMessageStorageAttribute), true).FirstOrDefault() != null;

            if (hasUseStorageAttribute && messageRepository == null)
            {
                logger.LogWarning("Contract class has UseStorageAttribute but no storage repository has been defined. Contract Type: {type}", entry.ContractClassType.FullName);
            }
            if (messageRepository != null && hasUseStorageAttribute)
            {
                var methodInfo = typeof(MessageDataConfiguratorExtensions).GetMethod("UseMessageData", BindingFlags.Static | BindingFlags.Public, null, [typeof(IBusFactoryConfigurator), typeof(IMessageDataRepository)], null);
                MethodInfo genericMethod = methodInfo.MakeGenericMethod(entry.ContractClassType);
                genericMethod.Invoke(c, new object[] { c, messageRepository });
            }
        }
        private static void SetupSubscriptionEndpoints(string defaultNameSpace, IRegistrationContext context, IServiceBusBusFactoryConfigurator cfg, IEnumerable<ConsumerEntry> topologyEntries, ILogger logger, IMessageDataRepository messageRepository)
        {
            foreach (var entry in topologyEntries)
            {
                if (!entry.ConsumerClassTypes.Any())
                    continue;

                logger?.LogInformation("Configuring subscription '{subscription}' on topic '{receiveEndPoint}' to handle contract '{contractName}'", defaultNameSpace, entry.ReceiveEndPoint, entry.ContractClassType);

                cfg.SubscriptionEndpoint(defaultNameSpace, entry.ReceiveEndPoint, c =>
                {
                    c.EnableDeadLetteringOnMessageExpiration = true;
                    c.MaxDeliveryCount = 3;
                    c.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1)));
                    foreach (Type cs in entry.ConsumerClassTypes)
                    {
                        c.ConfigureConsumer(context, cs);
                        logger?.LogInformation("Adding consumer '{consumerName}' to queue '{queueName}' to handle contract '{contractName}'", cs.Name, entry.ReceiveEndPoint, entry.ContractClassType.Name);
                    }
                });
            }
        }
    }
}
