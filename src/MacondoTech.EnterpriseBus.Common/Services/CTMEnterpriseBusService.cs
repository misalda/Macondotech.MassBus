using MacondoTech.EnterpriseBus.Common.Configuration;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MacondoTech.EnterpriseBus.Common.Services
{
    public class CTMEnterpriseBusService : ICTMEnterpriseBus
    {
        private readonly IBusControl _busControl;
        private readonly BusConfiguration _configuration;

        public string Name => _configuration?.TopologyMap.DefaultEndPoint;

        private readonly ILogger<ICTMEnterpriseBus> _logger;

        public CTMEnterpriseBusService(IBusControl busControl, BusConfiguration configuration, ILogger<ICTMEnterpriseBus> logger)
        {
            _busControl = busControl;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Send<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                ISendEndpoint sendEndpoint = await GetSendEndpoint<T>();

                _logger.LogInformation("Sending message: {messageType}, @{message}", typeof(T).Name, message);
                await sendEndpoint.Send(message, cancellationToken);
                _logger.LogInformation("Sent message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message: {messageType}, @{message}", typeof(T).Name, message);
                throw;
            }
            
        }

        private async Task<ISendEndpoint> GetSendEndpoint<T>() where T : class
        {
            var sendpointUri = new Uri($"{_configuration.AzureServiceBus.Uri}/{_configuration.TopologyMap.SendEndpoints[typeof(T)]}");
            var sendEndpoint = await _busControl.GetSendEndpoint(sendpointUri);
            return sendEndpoint;
        }

        public async Task Send<T>(object message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                ISendEndpoint sendEndpoint = await GetSendEndpoint<T>();

                _logger.LogInformation("Sending message: {messageType}, @{message}", typeof(T).Name, message);
                await sendEndpoint.Send<T>(message, cancellationToken);
                _logger.LogInformation("Sent message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message: {messageType}, @{message}", typeof(T).Name, message);
                throw;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("CTM service bus starting");
                return _busControl.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"CTM service bus start error");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("CTM service bus stopping");
                return _busControl.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"CTM service bus stop error");
                throw;
            }
        }
        public async Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogInformation("Publish event: {messageType}, @{message}", typeof(T).Name, message);
                await _busControl.Publish(message, cancellationToken);
                _logger.LogInformation("Published event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Publish event error. {messageType}, @{message}", typeof(T).Name, message);
                throw;
            }
        }

        public async Task Publish<T>(object message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogInformation("Publish event: {messageType}, @{message}", typeof(T).Name, message);
                await _busControl.Publish<T>(message, cancellationToken);
                _logger.LogInformation("Published event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Publish event error: {messageType}, @{message}", typeof(T).Name, message);
                throw;
            }
        }
    }
}
