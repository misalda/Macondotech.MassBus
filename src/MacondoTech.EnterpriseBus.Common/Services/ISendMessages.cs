using System.Threading;
using System.Threading.Tasks;

namespace MacondoTech.EnterpriseBus.Common.Services
{
    public interface ISendMessages
    {
        Task Send<T>(T message, CancellationToken cancellationToken = default) where T : class;
        Task Send<T>(object message, CancellationToken cancellationToken = default) where T : class;
    }
}
