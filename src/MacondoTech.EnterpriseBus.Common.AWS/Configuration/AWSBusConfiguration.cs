using MacondoTech.EnterpriseBus.Conventions;

namespace MacondoTech.EnterpriseBus.Common.AWS.Configuration
{
    public class AWSBusConfiguration
    {
        /// <summary>
        /// Gets the AWS settings.
        /// </summary>
        public AWSSettings AWSSettings { get; }

        /// <summary>
        /// Gets the message conventions (for messages, commands, and events).
        /// </summary>
        public TopologyMap TopologyMap { get; }

        public AWSBusConfiguration(INamespaceResolver? resolver = null, AWSSettings? awsSettings = null)
        {
            AWSSettings = awsSettings ?? new AWSSettings();
            TopologyMap = resolver != null ? resolver.BuildTopology() : new DefaultNamespaceResolver().BuildTopology();
        }
    }
}
