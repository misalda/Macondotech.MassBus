using NUnit.Framework;
using System.Linq;
using CTM.EnterpriseBus.UnitTests.Requests;
using CTM.EnterpriseBus.UnitTests.Events;
using CTM.EnterpriseBus.UnitTests.Consumers;
using CTM.EnterpriseBus.UnitTests.Messages;
using CTM.EnterpriseBus.Conventions;

namespace CTM.EnterpriseBus.UnitTests
{
    public class DefaultNameSpaceResolverTests
    {
        private TopologyMap _map;
        [OneTimeSetUp]
        public void Setup()
        {
            _map = new DefaultNamespaceResolver(contractNameSpace: "CTM.EnterpriseBus.UnitTests").BuildTopology();
        }

        [Test]
        public void TestEventConsumersEntriesCreated()
        {
            Assert.That(_map.EventConsumers.Count() == 2, Is.True, "event consumer entries  created");
           
        }
        [Test]
        public void TestEvent2MultipleConsumersClassesCreated()
        {
            Assert.That(_map.EventConsumers.Single(i => i.ContractClassType == typeof(Test1Event)).ConsumerClassTypes.Count() == 2, Is.True, $"one event consumer entries  created for { typeof(Test1Event).Name} with 2 consumers");
        }
        [Test]
        public void TestRequestConsumersEntriesCreated()
        {
            Assert.That(_map.RequestConsumers.Count() == 2,Is.True, "request consumer entries  created");

        }
        [Test]
        public void TestRequestConsumersClassEntriesCreated()
        {
            Assert.That(_map.RequestConsumers.Single(i => i.ContractClassType == typeof(Test1Request)).ConsumerClassTypes.Count() == 1,Is.True, $"one request consumer entries  created for { typeof(Test1Request).Name} with 1 consumer");
            Assert.That(_map.RequestConsumers.Single(i => i.ContractClassType == typeof(Test1Request)).ConsumerClassTypes.Single() == typeof(Test1RequestProcessor),Is.True, $"consumer for { typeof(Test1Request).Name} set to expected type");
        }
        [Test]
        public void TestMessageConsumersEntriesCreated()
        {
            Assert.That(_map.MessageConsumers.Count() == 1,Is.True, "event consumer entries  created");

        }
        [Test]
        public void TestMessageConsumerClassesEntriesCreated()
        {
            Assert.That(_map.MessageConsumers.Single(i => i.ContractClassType == typeof(Test1Message)).ConsumerClassTypes.Count() == 1,Is.True,$"one event Message entries  created for { typeof(Test1Message).Name} with 1 consumer");
            Assert.That(_map.MessageConsumers.Single(i => i.ContractClassType == typeof(Test1Message)).ConsumerClassTypes.Single() == typeof(Test1MessageProcessor),Is.True, $"consumer for { typeof(Test1Message).Name} set to expected type");
        }
    }
}