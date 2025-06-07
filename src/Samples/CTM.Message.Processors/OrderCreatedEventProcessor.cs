using System;
using MassTransit;
using System.Threading.Tasks;
using CTM.EnterpriseBus.Contracts.Events;

namespace CTM.Message.Processors
{
    public class OrderCreatedEventProcessor : IConsumer<OrderCreatedEvent>
    {
        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            await Console.Out.WriteLineAsync($"Order Created Event Received:{Environment.NewLine}--Order Id:{context.Message.OrderId}{Environment.NewLine}--Order Description:{context.Message.OrderDescription}");
            await Console.Out.WriteLineAsync($"Created DateTime:{context.Message.Timestamp}");
            // update the customer address
        }
    }
}