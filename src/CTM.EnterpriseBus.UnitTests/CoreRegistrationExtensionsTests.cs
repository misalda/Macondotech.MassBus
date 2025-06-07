using CTM.EnterpriseBus.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CTM.EnterpriseBus.Core.Extensions;
using CTM.EnterpriseBus.Common.Configuration;
using MassTransit;
using CTM.EnterpriseBus.UnitTests.Messages;
using CTM.EnterpriseBus.UnitTests.Events;
using CTM.EnterpriseBus.UnitTests.Consumers;

namespace CTM.EnterpriseBus.UnitTests
{
    [TestFixture]
    public class CoreRegistrationExtensionsTests
    {
        
        private ILogger _logger;
        private AzureServiceBusSettings _sbSettings;
        [OneTimeSetUp]
        public void Setup()
        {
            _logger =new Mock<ILogger>().Object;
            _sbSettings = new AzureServiceBusSettings() { Uri = "https://localhost:8080", KeyName = "KeyName", SharedAccessKey = "Accesskey" };


        }
        [Test]
        public void Consumers_are_registered_as_expected()
        {
            //create service collection
            var services = new ServiceCollection();
            //call registration code with mock settings and test econsumers and contracts
            services.AddCTMServiceBus(new BusConfiguration(new DefaultNamespaceResolver(contractNameSpace:"CTM.EnterpriseBus.UnitTests"),_sbSettings), _logger);
            var provider = services.BuildServiceProvider();
            Assert.That(provider.GetService<Test1EventProcessor>(), Is.Not.Null);
            Assert.That(provider.GetService<Test1EventProcessor2>(), Is.Not.Null);
            Assert.That(provider.GetService<Test2EventProcessor>(), Is.Not.Null);
            Assert.That(provider.GetService<Test1MessageProcessor>(), Is.Not.Null);
            Assert.That(provider.GetService<Test1RequestProcessor>(), Is.Not.Null);
            Assert.That(provider.GetService<Test1RequestProcessor>(), Is.Not.Null);

        }
    }

}
