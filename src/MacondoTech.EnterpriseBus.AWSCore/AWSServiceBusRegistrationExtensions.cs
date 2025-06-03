using Amazon.Runtime;
using MacondoTech.EnterpriseBus.Common.AWS.Configuration;
using MacondoTech.EnterpriseBus.Common.Services; // For IMessageDataRepository if needed
using MacondoTech.EnterpriseBus.Conventions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MacondoTech.EnterpriseBus.AWSCore.Extensions
{
    public static class AWSServiceBusRegistrationExtensions
    {
        public static void AddAWSServiceBus(this IServiceCollection services, AWSBusConfiguration awsBusConfiguration, ILogger logger = null)
        {
            if (awsBusConfiguration == null) throw new ArgumentNullException(nameof(awsBusConfiguration));
            if (awsBusConfiguration.AWSSettings == null) throw new ArgumentNullException(nameof(awsBusConfiguration.AWSSettings));

            if (!awsBusConfiguration.AWSSettings.IsValid())
            {
                logger?.LogError("AWS Settings are not properly configured. Region, AccessKey, and SecretKey are required.");
                throw new ArgumentException("AWS Settings are not properly configured.", nameof(awsBusConfiguration.AWSSettings));
            }

            services.AddMassTransit(x =>
            {
                var map = awsBusConfiguration.TopologyMap;

                // Register all consumers
                foreach (var consumerType in map.EventConsumers.SelectMany(c => c.ConsumerClassTypes))
                {
                    x.AddConsumer(consumerType);
                    logger?.LogInformation("Adding event consumer {ConsumerName}", consumerType.FullName);
                }

                foreach (var consumerType in map.MessageConsumers.SelectMany(c => c.ConsumerClassTypes))
                {
                    x.AddConsumer(consumerType);
                    logger?.LogInformation("Adding message consumer '{ConsumerName}'", consumerType.FullName);
                }

                foreach (var consumerType in map.RequestConsumers.SelectMany(c => c.ConsumerClassTypes))
                {
                    x.AddConsumer(consumerType);
                    logger?.LogInformation("Adding request consumer '{ConsumerName}'", consumerType.FullName);
                }

                x.UsingAmazonSQS((context, cfg) =>
                {
                    cfg.Host(awsBusConfiguration.AWSSettings.Region, h =>
                    {
                        // AWS Credentials
                        if (!string.IsNullOrEmpty(awsBusConfiguration.AWSSettings.AccessKey) &&
                            !string.IsNullOrEmpty(awsBusConfiguration.AWSSettings.SecretKey))
                        {
                            if (!string.IsNullOrEmpty(awsBusConfiguration.AWSSettings.SessionToken))
                            {
                                h.Credentials(new SessionAWSCredentials(awsBusConfiguration.AWSSettings.AccessKey, awsBusConfiguration.AWSSettings.SecretKey, awsBusConfiguration.AWSSettings.SessionToken));
                                logger?.LogInformation("Configuring AWS Host with AccessKey, SecretKey, and SessionToken.");
                            }
                            else
                            {
                                h.Credentials(new BasicAWSCredentials(awsBusConfiguration.AWSSettings.AccessKey, awsBusConfiguration.AWSSettings.SecretKey));
                                logger?.LogInformation("Configuring AWS Host with AccessKey and SecretKey.");
                            }
                        }
                        else
                        {
                            logger?.LogInformation("Configuring AWS Host with default credentials (e.g., IAM role).");
                        }
                    });

                    logger?.LogInformation("Connected to Amazon SQS in region '{Region}'", awsBusConfiguration.AWSSettings.Region);

                    // Placeholder for IMessageDataRepository - check if needed for AWS (e.g., S3)
                    // var messageRepository = context.GetService<IMessageDataRepository>(); // GetService to allow it to be optional
                    // if (messageRepository != null)
                    // {
                    //     cfg.UseMessageData(messageRepository); // This might need an S3 specific implementation
                    //     logger?.LogInformation("Using message repository '{messageRepository}'", messageRepository.GetType().Name);
                    // }

                    // Configure endpoints for Events (SNS topics via SQS queues)
                    // MassTransit by convention creates an SQS queue for each event type consumer group
                    // and subscribes it to the corresponding SNS topic.
                    // The queue name is typically derived from the consumer/endpoint name.
                    // We need a unique queue name for each subscription group if not using default conventions.
                    SetupSqsQueuesForTopics(map.DefaultEndPoint, context, cfg, map.EventConsumers, logger);


                    // Configure endpoints for Messages (SQS Queues)
                    SetupSqsQueues(context, cfg, map.MessageConsumers, logger);

                    // Configure endpoints for Requests (SQS Queues)
                    SetupSqsQueues(context, cfg, map.RequestConsumers, logger);
                });

                // Configure Request Clients
                var addRequestClientMethod = typeof(MassTransit.Registration.RegistrationExtensions).GetMethods()
                    .First(m => m.Name == "AddRequestClient" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(Type));

                foreach (var endpoint in map.RequestClientEndpoints)
                {
                    // Key is the request type, Value is the queue name
                    Type requestType = endpoint.Key;
                    string queueName = endpoint.Value;
                    Uri requestUri = new Uri($"queue:{queueName}"); // SQS queues are addressed directly

                    // It seems AddRequestClient(Type, Uri, RequestTimeout) is more appropriate here if available
                    // Or use the IRegistrationConfigurator overload x.AddRequestClient(Type, destinationAddress, timeout)
                    var specificAddRequestClientMethod = addRequestClientMethod.MakeGenericMethod(requestType);
                    specificAddRequestClientMethod.Invoke(x, new object[] { x, requestUri, RequestTimeout.Default });


                    logger?.LogInformation("Configuring request client for {RequestType} to queue {QueueName}", requestType.FullName, queueName);
                }
            });

            });

            // Ensure AWSBusConfiguration is available as a singleton
            services.AddSingleton(awsBusConfiguration);
            // Register the AWSEnterpriseBusService for the ICTMEnterpriseBus interface
            services.AddSingleton<ICTMEnterpriseBus, AWSEnterpriseBusService>();
        }

        private static void SetupSqsQueues(
            IRegistrationContext context,
            IAmazonSqsBusFactoryConfigurator cfg,
            IEnumerable<ConsumerEntry> consumerEntries,
            ILogger logger)
        {
            foreach (var entry in consumerEntries)
            {
                if (!entry.ConsumerClassTypes.Any()) continue;

                var queueName = entry.ReceiveEndPoint; // Assuming ReceiveEndPoint from TopologyMap is the SQS queue name
                logger?.LogInformation("Configuring SQS queue '{QueueName}' for contract '{ContractName}'", queueName, entry.ContractClassType.FullName);

                cfg.ReceiveEndpoint(queueName, c =>
                {
                    c.ConfigureDeadLetterQueueDeadLetterTransport(); // Configure DLQ
                    c.ConfigureDeadLetterQueueErrorTransport();
                    // c.EnableDeadLetteringOnMessageExpiration = true; // SQS handles this via Redrive Policy
                    c.MaxDeliveryCount = 3; // Example, should be configurable or match SQS redrive policy
                    c.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5))); // Example retry

                    foreach (Type consumerType in entry.ConsumerClassTypes)
                    {
                        c.ConfigureConsumer(context, consumerType);
                        logger?.LogInformation("Adding consumer '{ConsumerName}' to SQS queue '{QueueName}' for contract '{ContractName}'", consumerType.Name, queueName, entry.ContractClassType.Name);
                    }
                });
            }
        }

        private static void SetupSqsQueuesForTopics(
            string defaultSubscriptionNamePrefix, // Used to generate SQS queue names for SNS subscriptions
            IRegistrationContext context,
            IAmazonSqsBusFactoryConfigurator cfg,
            IEnumerable<ConsumerEntry> eventConsumerEntries,
            ILogger logger)
        {
            foreach (var entry in eventConsumerEntries)
            {
                if (!entry.ConsumerClassTypes.Any()) continue;

                // For SNS, ReceiveEndPoint is the topic name.
                // MassTransit creates an SQS queue and subscribes it to the SNS topic.
                // The queue name needs to be unique for each application/service instance group that wants to receive all events.
                // Or, if multiple different consumers for the same event type exist in the same service, they might share a queue.
                // Conventionally, MassTransit forms a queue name like: YourService_YourEvent_subscribe
                // Or it uses the endpoint name provided. Let's use DefaultEndpoint from topology + TopicName.
                var topicName = entry.ReceiveEndPoint; // This should be the SNS topic name
                var queueName = $"{defaultSubscriptionNamePrefix}_{topicName}"; // Construct a unique SQS queue name for the subscription
                                                                            // MassTransit will subscribe this SQS queue to the SNS topic named `topicName` (or derived from event type)

                logger?.LogInformation("Configuring SQS queue '{QueueName}' to subscribe to SNS topic (derived from contract) '{TopicName}' for event contract '{ContractName}'",
                                       queueName, topicName, entry.ContractClassType.FullName);

                cfg.ReceiveEndpoint(queueName, c =>
                {
                    // c.Subscribe(topicName); // MassTransit does this by convention if the message type is an event and queue is for topics
                                            // Or configure topic subscriptions explicitly if needed.
                                            // For events, MassTransit automatically subscribes the endpoint queue to the topic
                                            // matching the event type name (or as configured by conventions).

                    c.ConfigureDeadLetterQueueDeadLetterTransport();
                    c.ConfigureDeadLetterQueueErrorTransport();
                    c.MaxDeliveryCount = 3; // Should align with SQS redrive policy
                    c.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));

                    // Important: Configure the topic subscription if MassTransit doesn't do it by convention for all cases
                    // This is usually needed if the topic name doesn't exactly match the event type name convention
                    // or if you need specific subscription attributes.
                    // For event types, MassTransit automatically creates the topic if it doesn't exist
                    // and subscribes the endpoint queue to it.
                    // c.ConfigureConsumeTopology = false; // Set this if you manage SNS/SQS topology manually

                    foreach (Type consumerType in entry.ConsumerClassTypes)
                    {
                        c.ConfigureConsumer(context, consumerType);
                        logger?.LogInformation("Adding consumer '{ConsumerName}' to SQS queue '{QueueName}' (for SNS topic '{TopicName}') for event '{EventName}'",
                                               consumerType.Name, queueName, topicName, entry.ContractClassType.Name);
                    }
                });
            }
        }
    }
}
