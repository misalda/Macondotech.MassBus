using MacondoTech.EnterpriseBus.Contracts.Requests;
using MacondoTech.EnterpriseBus.Contracts.Responses;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace MacondoTech.Message.Processors
{
    public class OrderSubmitedProcessor : IConsumer<SubmitOrderRequest>
    {
        public async Task Consume(ConsumeContext<SubmitOrderRequest> context)
        {
            Console.WriteLine($"Order Submited --> {context.Message.OrderDescription}");
            await context.RespondAsync<SubmitOrderResponse>(new { OrderId = Guid.NewGuid(), OrderDescription = context.Message.OrderDescription, CreationTime =DateTime.Now});
            // update the customer address
        }
    }
}