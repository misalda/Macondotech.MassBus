using System;
using Azure;

namespace MacondoTech.EnterpriseBus.Common.Configuration
{
    public class AzureServiceBusSettings
    {
        public string Uri { get; set; }
        public string KeyName { get; set; }
        public string SharedAccessKey { get; set; }

        public string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(Uri) || string.IsNullOrEmpty(KeyName) || string.IsNullOrEmpty(SharedAccessKey))
                {
                    throw new ArgumentException("Azure Service Bus settings are not properly configured.");
                }
                return $"Endpoint={Uri};SharedAccessKeyName={KeyName};SharedAccessKey={SharedAccessKey}";
            }
        }
    }
}
