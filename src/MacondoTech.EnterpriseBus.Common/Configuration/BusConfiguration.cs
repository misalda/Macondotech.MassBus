using MacondoTech.EnterpriseBus.Conventions;

namespace MacondoTech.EnterpriseBus.Common.Configuration
{
    public class BusConfiguration
    {
        /// <summary>
        /// Gets the azure service bus settings
        /// </summary>
        public AzureServiceBusSettings AzureServiceBus { get;}

        /// <summary>
        /// Gets the message conventions (for messages, commands, and events)
        /// </summary>
        public TopologyMap TopologyMap {get;}

        public BusConfiguration(INamespaceResolver resolver = null, AzureServiceBusSettings azureServiceBusSettings = null)
        {
            AzureServiceBus = azureServiceBusSettings??new AzureServiceBusSettings();
            TopologyMap = resolver!= null ? resolver.BuildTopology() : new DefaultNamespaceResolver().BuildTopology();
        }
    }
}
