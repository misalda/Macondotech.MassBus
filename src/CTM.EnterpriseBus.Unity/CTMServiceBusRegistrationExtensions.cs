using CTM.EnterpriseBus.Common.Configuration;
using CTM.EnterpriseBus.Common.Services;
using CTM.EnterpriseBus.Conventions;
using Azure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity;
using MassTransit.UnityIntegration;
using MassTransit;
using Unity.Lifetime;
using Unity.Injection;
using MassTransit.MessageData;

namespace CTM.EnterpriseBus.Unity.Extensions
{
    public static class CTMServiceBusRegistrationExtensions
    {
        public static void AddCTMServiceBus(this UnityContainer unityContainer, BusConfiguration busConfiguration, ILogger logger = null, IMessageDataRepository messageRepository=null)
        {
            var map = busConfiguration.TopologyMap;
            foreach (var consumerType in map.EventConsumers.SelectMany(i => i.ConsumerClassTypes))
            {
                unityContainer.RegisterType(consumerType, new ContainerControlledLifetimeManager());
            }

            foreach (var msgConsumer in map.MessageConsumers.SelectMany(i => i.ConsumerClassTypes))
            {
                unityContainer.RegisterType(msgConsumer, new ContainerControlledLifetimeManager());
            }
            foreach (var reqConsumer in map.RequestConsumers.SelectMany(i => i.ConsumerClassTypes))
            {
                unityContainer.RegisterType(reqConsumer, new ContainerControlledLifetimeManager());
            }
        
            var busControl = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                cfg.RequiresSession = false;
                cfg.MaxConcurrentCalls = 500;
                cfg.SessionIdleTimeout = TimeSpan.FromMinutes(5);
                var sasCredential = new AzureSasCredential(busConfiguration.AzureServiceBus.SharedAccessKey);
                cfg.Host(new Uri(busConfiguration.AzureServiceBus.Uri), h =>
                {
                    //h.OperationTimeout = TimeSpan.FromSeconds(5);
                    h.SharedAccessSignature(s =>
                    {
                        h.SasCredential= sasCredential;
                    });
                });
                SetupSubscriptionEndpoints(map.DefaultEndPoint, unityContainer, cfg, map.EventConsumers, logger, messageRepository);
                SetupQueueEndpoints(unityContainer, cfg, map.MessageConsumers, logger, messageRepository);
                SetupQueueEndpoints(unityContainer, cfg, map.RequestConsumers, logger, messageRepository);
            });

            unityContainer.ReqisterRequestClients(map.RequestClientEndpoints);

            unityContainer.RegisterInstance<IBusControl>(busControl);
            unityContainer.RegisterInstance<IBus>(busControl);
            unityContainer.RegisterInstance(busConfiguration);
            unityContainer.RegisterType<ICTMEnterpriseBus, CTMEnterpriseBusService>();
        }

        

        private static void SetupQueueEndpoints(UnityContainer unityContainer, IServiceBusBusFactoryConfigurator cfg, IEnumerable<ConsumerEntry> consumerEntries, ILogger logger, IMessageDataRepository messageRepository)
        {

            foreach (var entry in consumerEntries)
            {
                if (!entry.ConsumerClassTypes.Any())
                    continue;

                logger?.LogInformation("Configuring queue {queueName} to handle contract {contractName}", entry.ReceiveEndPoint, entry.ContractClassType);


                cfg.ReceiveEndpoint(entry.ReceiveEndPoint, c =>
                {
                    c.ConfigureMessageRepository(messageRepository, entry, logger);
                    c.EnableDeadLetteringOnMessageExpiration = true;
                    //c.SubscribeMessageTopics = false;
                    c.MaxDeliveryCount = 3;
                    c.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1)));
                    c.RegisterConsumer(unityContainer,entry.ConsumerClassTypes.ToArray());
                });
            }
        }        
        private static void SetupSubscriptionEndpoints(string defaultNameSpace, UnityContainer unityContainer, IServiceBusBusFactoryConfigurator cfg, IEnumerable<ConsumerEntry> topologyEntries, ILogger logger, IMessageDataRepository messageRepository)
        {
            foreach (var entry in topologyEntries)
            {
                if (!entry.ConsumerClassTypes.Any())
                    continue;

                logger?.LogInformation("Configuring subscription {subscription} on topic {receiveEndPoint} to handle contract {contractName}", defaultNameSpace, entry.ReceiveEndPoint, entry.ContractClassType);

                cfg.SubscriptionEndpoint(defaultNameSpace, entry.ReceiveEndPoint, c => 
                {
                    c.ConfigureMessageRepository(messageRepository, entry, logger);
                    c.EnableDeadLetteringOnMessageExpiration = true;
                    c.MaxDeliveryCount = 3;
                    c.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1)));
                    c.RegisterConsumer(unityContainer,entry.ConsumerClassTypes.ToArray());
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
                var methodInfo = typeof(MessageDataConfiguratorExtensions).GetMethod("UseMessageData", new[] { typeof(IConsumePipeConfigurator), typeof(IMessageDataRepository) });
                MethodInfo genericMethod = methodInfo.MakeGenericMethod(entry.ContractClassType);
                genericMethod.Invoke(c, new object[] { c, messageRepository });
            }
        }
        
    }
}
