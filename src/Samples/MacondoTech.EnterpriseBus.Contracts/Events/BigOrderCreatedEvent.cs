using System;
using MacondoTech.EnterpriseBus.Conventions;
using MassTransit;
namespace MacondoTech.EnterpriseBus.Contracts.Events
{
    [UseMessageStorage]
    public interface BigOrderCreatedEvent
    {
        Guid OrderId { get; }
        string OrderDescription { get; }
        DateTime Timestamp { get; }
        DateTime OrderCreatedDateTime { get; }
        MessageData<byte[]> BigData { get; }
    }
}