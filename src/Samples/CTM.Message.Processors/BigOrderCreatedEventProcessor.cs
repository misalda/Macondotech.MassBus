using MassTransit;
using System.Threading.Tasks;
using System;
using CTM.EnterpriseBus.Contracts.Responses;
using CTM.EnterpriseBus.Contracts.Events;
using System.IO;

namespace CTM.Message.Processors
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