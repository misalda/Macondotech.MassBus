using MacondoTech.EnterpriseBus.Common.Configuration;
using MacondoTech.EnterpriseBus.Common.Services;
using MacondoTech.EnterpriseBus.Conventions;
using MacondoTech.EnterpriseBus.UnitTests.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MacondoTech.EnterpriseBus.UnitTests.CTMServiceBus
{
    [TestFixture]
    public class CTMServiceBusTests
    {
        [Test]
        public async Task SendLogicUsesTopologyMapForEndpoint()
        {
            var busControlMock = new Mock<IBusControl>();
            var logger = new Mock<ILogger<ICTMEnterpriseBus>>();
            var sendEndpointMock = new Mock<ISendEndpoint>();

            busControlMock.Setup(bus => bus.GetSendEndpoint(It.IsAny<Uri>())).ReturnsAsync(sendEndpointMock.Object).Verifiable();
            //setup nameresolver using tests contracts in project
            var resolver = new DefaultNamespaceResolver(contractNameSpace: "CTM.EnterpriseBus.UnitTests");
            //build the map 
            var _map = resolver.BuildTopology();
            //initialize configuration using resolver
            var _busConfiguration = new BusConfiguration(resolver,new AzureServiceBusSettings() { Uri = "https://localhost", KeyName = "something", SharedAccessKey = "something" });
            var _busService = new CTMEnterpriseBusService(busControlMock.Object, _busConfiguration, logger.Object); ;
            Console.WriteLine(_map.SendEndpoints[typeof(Test1Message)]);
            await _busService.Send(new Test1Message{ MyProperty = 1 });
            busControlMock.Verify(bus => bus.GetSendEndpoint(It.Is<Uri>(u=>u.Equals(new Uri($"{_busConfiguration.AzureServiceBus.Uri}/{_map.SendEndpoints[typeof(Test1Message)]}")))), Times.Once());
            sendEndpointMock.Verify(ep => ep.Send(It.IsAny<Test1Message>(),default), Times.Once());
            Assert.Pass("Send message logic uses endpoint specified by topology map");
        }
    }


}
