using System;
namespace MacondoTech.EnterpriseBus.Contracts.Events
{
    public interface OrderCreatedEvent
    {
        Guid OrderId { get; }
        string OrderDescription { get; }
        DateTime Timestamp { get; }
        DateTime OrderCreatedDateTime { get; }
    }
}