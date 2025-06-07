using Azure;

namespace CTM.EnterpriseBus.Common.Configuration
{
    public class AzureServiceBusSettings
    {
        public string Uri { get; set; }
        public string KeyName { get; set; }
        public string SharedAccessKey { get; set; }
    }
}
