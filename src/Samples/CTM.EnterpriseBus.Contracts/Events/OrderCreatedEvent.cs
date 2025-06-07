using System;
namespace CTM.EnterpriseBus.Contracts.Events
{
    public interface OrderCreatedEvent
    {
        Guid OrderId { get; }
        string OrderDescription { get; }
        DateTime Timestamp { get; }
        DateTime OrderCreatedDateTime { get; }
    }
}