using Microsoft.Extensions.Hosting;

namespace MacondoTech.EnterpriseBus.Common.Services
{
    public interface ICTMEnterpriseBus : ISendMessages, IPublishMessages, IHostedService
    {
        string Name { get; }
    }
}
