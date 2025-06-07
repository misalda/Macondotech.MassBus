using Microsoft.Extensions.Hosting;

namespace CTM.EnterpriseBus.Common.Services
{
    public interface ICTMEnterpriseBus : ISendMessages, IPublishMessages, IHostedService
    {
        string Name { get; }
    }
}
