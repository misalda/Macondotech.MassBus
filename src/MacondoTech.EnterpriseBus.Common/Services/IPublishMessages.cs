using System.Threading;
using System.Threading.Tasks;

namespace MacondoTech.EnterpriseBus.Common.Services
{
    public interface IPublishMessages
    {
        Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class;
        Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class;
    }
}