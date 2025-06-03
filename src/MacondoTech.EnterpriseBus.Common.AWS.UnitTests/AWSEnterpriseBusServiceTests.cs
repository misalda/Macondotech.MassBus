using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MacondoTech.EnterpriseBus.Common.AWS.Configuration;
using MacondoTech.EnterpriseBus.Common.AWS.Services;
using MacondoTech.EnterpriseBus.Conventions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MacondoTech.EnterpriseBus.Common.AWS.UnitTests
{
    public class AWSEnterpriseBusServiceTests
    {
        private readonly Mock<IBusControl> _mockBusControl;
        private readonly Mock<AWSBusConfiguration> _mockBusConfiguration;
        private readonly Mock<AWSSettings> _mockAwsSettings;
        private readonly Mock<ILogger<AWSEnterpriseBusService>> _mockLogger;
        private readonly Mock<TopologyMap> _mockTopologyMap;

        public AWSEnterpriseBusServiceTests()
        {
            _mockBusControl = new Mock<IBusControl>();
            _mockAwsSettings = new Mock<AWSSettings>();
            _mockTopologyMap = new Mock<TopologyMap>();
            _mockBusConfiguration = new Mock<AWSBusConfiguration>(null, _mockAwsSettings.Object); // Pass null for resolver, use mocked AWSSettings
            _mockBusConfiguration.Setup(c => c.TopologyMap).Returns(_mockTopologyMap.Object);
            _mockBusConfiguration.Setup(c => c.AWSSettings).Returns(_mockAwsSettings.Object);
            _mockLogger = new Mock<ILogger<AWSEnterpriseBusService>>();

            // Default setup for valid AWS settings
            _mockAwsSettings.Setup(s => s.IsValid()).Returns(true);
            _mockAwsSettings.Setup(s => s.Region).Returns("us-east-1");
            _mockAwsSettings.Setup(s => s.AccessKey).Returns("testAccessKey");
            _mockAwsSettings.Setup(s => s.SecretKey).Returns("testSecretKey");
        }

        private AWSEnterpriseBusService CreateService()
        {
            return new AWSEnterpriseBusService(
                _mockBusControl.Object,
                _mockBusConfiguration.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenBusControlIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AWSEnterpriseBusService(null!, _mockBusConfiguration.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenConfigurationIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AWSEnterpriseBusService(_mockBusControl.Object, null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AWSEnterpriseBusService(_mockBusControl.Object, _mockBusConfiguration.Object, null!));
        }

        [Fact]
        public void Constructor_ThrowsArgumentException_WhenAWSSettingsAreInvalid()
        {
            _mockAwsSettings.Setup(s => s.IsValid()).Returns(false);
            Assert.Throws<ArgumentException>(() => CreateService());
        }

        [Fact]
        public void Constructor_SetsNameProperty_FromTopologyMap()
        {
            _mockTopologyMap.Setup(t => t.DefaultEndPoint).Returns("TestEndpoint");
            var service = CreateService();
            Assert.Equal("TestEndpoint", service.Name);
        }

        [Fact]
        public async Task Send_Generic_CallsBusControlGetSendEndpointAndSendsMessage()
        {
            // Arrange
            var message = new TestMessage { Content = "Hello" };
            var queueName = "test-queue";
            var expectedUri = new Uri($"queue:{queueName}");
            _mockTopologyMap.Setup(t => t.SendEndpoints).Returns(new Dictionary<Type, string> { { typeof(TestMessage), queueName } });
            var mockSendEndpoint = new Mock<ISendEndpoint>();
            _mockBusControl.Setup(b => b.GetSendEndpoint(expectedUri)).ReturnsAsync(mockSendEndpoint.Object);

            var service = CreateService();

            // Act
            await service.Send(message);

            // Assert
            _mockBusControl.Verify(b => b.GetSendEndpoint(expectedUri), Times.Once);
            mockSendEndpoint.Verify(s => s.Send(message, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Send_Object_CallsBusControlGetSendEndpointAndSendsMessage()
        {
            // Arrange
            var message = new TestMessage { Content = "Hello Object" };
            var queueName = "test-object-queue";
            var expectedUri = new Uri($"queue:{queueName}");
            _mockTopologyMap.Setup(t => t.SendEndpoints).Returns(new Dictionary<Type, string> { { typeof(TestMessage), queueName } });
            var mockSendEndpoint = new Mock<ISendEndpoint>();
            _mockBusControl.Setup(b => b.GetSendEndpoint(expectedUri)).ReturnsAsync(mockSendEndpoint.Object);

            var service = CreateService();

            // Act
            await service.Send<TestMessage>(message);

            // Assert
            _mockBusControl.Verify(b => b.GetSendEndpoint(expectedUri), Times.Once);
            mockSendEndpoint.Verify(s => s.Send<TestMessage>(message, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Send_Generic_ThrowsArgumentException_WhenMessageTypeNotMapped()
        {
            _mockTopologyMap.Setup(t => t.SendEndpoints).Returns(new Dictionary<Type, string>()); // Empty map
            var service = CreateService();
            var message = new UnmappedMessage();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.Send(message));
            Assert.Contains("Send endpoint not configured for message type", ex.Message);
        }

        [Fact]
        public async Task Send_Generic_ThrowsArgumentNullException_WhenMessageIsNull()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.Send<TestMessage>(null!));
        }

        [Fact]
        public async Task Publish_Generic_CallsBusControlPublish()
        {
            // Arrange
            var message = new TestEvent { EventData = "Boom!" };
            var service = CreateService();

            // Act
            await service.Publish(message);

            // Assert
            _mockBusControl.Verify(b => b.Publish(message, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Publish_Object_CallsBusControlPublish()
        {
            // Arrange
            var message = new TestEvent { EventData = "Boom Object!" };
            var service = CreateService();

            // Act
            await service.Publish<TestEvent>(message);

            // Assert
            _mockBusControl.Verify(b => b.Publish<TestEvent>(message, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Publish_Generic_ThrowsArgumentNullException_WhenMessageIsNull()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.Publish<TestEvent>(null!));
        }

        [Fact]
        public async Task StartAsync_CallsBusControlStartAsync()
        {
            var service = CreateService();
            await service.StartAsync(CancellationToken.None);
            _mockBusControl.Verify(b => b.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StopAsync_CallsBusControlStopAsync()
        {
            var service = CreateService();
            await service.StopAsync(CancellationToken.None);
            _mockBusControl.Verify(b => b.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    // Helper classes for testing
    public class TestMessage { public string? Content { get; set; } }
    public class UnmappedMessage { public string? Data { get; set; } }
    public class TestEvent { public string? EventData { get; set; } }
}
