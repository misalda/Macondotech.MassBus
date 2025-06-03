namespace MacondoTech.EnterpriseBus.Common.AWS.Configuration
{
    public class AWSSettings
    {
        public string Region { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        // Optional: SessionToken for temporary credentials
        public string? SessionToken { get; set; }

        public AWSSettings()
        {
            // Initialize with default values or leave empty for DI/configuration binding
            Region = string.Empty;
            AccessKey = string.Empty;
            SecretKey = string.Empty;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Region) &&
                   !string.IsNullOrEmpty(AccessKey) &&
                   !string.IsNullOrEmpty(SecretKey);
        }
    }
}
