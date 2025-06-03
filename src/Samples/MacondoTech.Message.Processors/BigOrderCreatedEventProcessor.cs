using MassTransit;
using System.Threading.Tasks;
using System;
using MacondoTech.EnterpriseBus.Contracts.Responses;
using MacondoTech.EnterpriseBus.Contracts.Events;
using System.IO;

namespace MacondoTech.Message.Processors
{
    public class BigOrderCreatedEventProcessor : IConsumer<BigOrderCreatedEvent>
    {
        public async Task Consume(ConsumeContext<BigOrderCreatedEvent> context)
        {
            if (context.Message.BigData.HasValue) {
                var myValue = await context.Message.BigData.Value;
                using (var stream = new MemoryStream(myValue))
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    Console.WriteLine(text);
                }
            }

            // update the customer address
        }
    }
}