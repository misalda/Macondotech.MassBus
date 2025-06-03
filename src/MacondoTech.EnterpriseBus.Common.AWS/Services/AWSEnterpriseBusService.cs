using MacondoTech.EnterpriseBus.Common.AWS.Configuration;
using MacondoTech.EnterpriseBus.Common.Services; // For ICTMEnterpriseBus, IPublishMessages, ISendMessages
using MacondoTech.EnterpriseBus.Conventions; // For TopologyMap
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MacondoTech.EnterpriseBus.Common.AWS.Services
{
    public class AWSEnterpriseBusService : ICTMEnterpriseBus // Implements ISendMessages, IPublishMessages, IHostedService
    {
        private readonly IBusControl _busControl;
        private readonly AWSBusConfiguration _configuration;
        private readonly ILogger<AWSEnterpriseBusService> _logger;

        public string Name => _configuration?.TopologyMap.DefaultEndPoint ?? "MacondoTech.AWS.DefaultEndpoint";

        public AWSEnterpriseBusService(IBusControl busControl, AWSBusConfiguration configuration, ILogger<AWSEnterpriseBusService> logger)
        {
            _busControl = busControl ?? throw new ArgumentNullException(nameof(busControl));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (_configuration.AWSSettings == null || !_configuration.AWSSettings.IsValid())
            {
                _logger.LogError("AWS Settings are not properly configured.");
                throw new ArgumentException("AWS Settings are not properly configured.", nameof(configuration.AWSSettings));
            }
        }

        public async Task Send<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                if (message == null) throw new ArgumentNullException(nameof(message));

                var destinationAddress = GetSendEndpointAddress<T>();
                _logger.LogInformation("Getting send endpoint for {DestinationAddress}", destinationAddress);
                ISendEndpoint sendEndpoint = await _busControl.GetSendEndpoint(destinationAddress);

                _logger.LogInformation("Sending message: {MessageType} to {DestinationAddress}, {@Message}", typeof(T).FullName, destinationAddress, message);
                await sendEndpoint.Send(message, cancellationToken);
                _logger.LogInformation("Sent message: {MessageType} to {DestinationAddress}", typeof(T).FullName, destinationAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message: {MessageType}, {@Message}", typeof(T).FullName, message);
                throw;
            }
        }

        public async Task Send<T>(object message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                if (message == null) throw new ArgumentNullException(nameof(message));

                var destinationAddress = GetSendEndpointAddress<T>();
                 _logger.LogInformation("Getting send endpoint for {DestinationAddress}", destinationAddress);
                ISendEndpoint sendEndpoint = await _busControl.GetSendEndpoint(destinationAddress);

                _logger.LogInformation("Sending message: {MessageType} to {DestinationAddress}, {@Message}", typeof(T).FullName, destinationAddress, message);
                await sendEndpoint.Send<T>(message, cancellationToken);
                _logger.LogInformation("Sent message: {MessageType} to {DestinationAddress}", typeof(T).FullName, destinationAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message: {MessageType}, {@Message}", typeof(T).FullName, message);
                throw;
            }
        }

        private Uri GetSendEndpointAddress<T>() where T : class
        {
            if (!_configuration.TopologyMap.SendEndpoints.TryGetValue(typeof(T), out var queueName))
            {
                _logger.LogError("Send endpoint not found for message type {MessageType}", typeof(T).FullName);
                throw new ArgumentException($"Send endpoint not configured for message type {typeof(T).FullName}. Ensure it is registered in TopologyMap.SendEndpoints.");
            }
            // For SQS, the address is just the queue name. MassTransit handles the full ARN construction.
            // However, MassTransit's GetSendEndpoint usually expects a full URI like 'queue:queueName' or 'topic:topicName'.
            // Let's assume queueName from TopologyMap is just the queue name for now.
            // We might need to adjust this based on how MassTransit SQS addressing works,
            // potentially prepending "queue:" or "topic:" if needed, or relying on MassTransit's conventions.
            // For SQS, it's typically just the queue name if the bus is configured with a base URI.
            // If not, it might need 'amazonsqs://host/queueName'.
            // Given MassTransit's IBusControl.GetSendEndpoint, a simple queue name might resolve if a convention is set up.
            // Or, more robustly, "queue:{queueName}" or "topic:{topicName}"
            return new Uri($"queue:{queueName}");
        }

        public async Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                if (message == null) throw new ArgumentNullException(nameof(message));
                _logger.LogInformation("Publishing event: {MessageType}, {@Message}", typeof(T).FullName, message);
                await _busControl.Publish(message, cancellationToken);
                _logger.LogInformation("Published event: {MessageType}", typeof(T).FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Publish event error: {MessageType}, {@Message}", typeof(T).FullName, message);
                throw;
            }
        }

        public async Task Publish<T>(object message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                if (message == null) throw new ArgumentNullException(nameof(message));
                _logger.LogInformation("Publishing event: {MessageType}, {@Message}", typeof(T).FullName, message);
                await _busControl.Publish<T>(message, cancellationToken);
                _logger.LogInformation("Published event: {MessageType}", typeof(T).FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Publish event error: {MessageType}, {@Message}", typeof(T).FullName, message);
                throw;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("AWSEnterpriseBusService starting...");
                return _busControl.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AWSEnterpriseBusService start error");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("AWSEnterpriseBusService stopping...");
                return _busControl.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AWSEnterpriseBusService stop error");
                throw;
            }
        }
    }
}
