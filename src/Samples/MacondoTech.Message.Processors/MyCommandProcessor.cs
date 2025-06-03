using System;
using MassTransit;
using System.Threading.Tasks;
using MacondoTech.EnterpriseBus.Contracts.Messages;

namespace MacondoTech.Message.Processors
{
    public class MyCommandProcessor : IConsumer<MyMessage>
    {
        public async Task Consume(ConsumeContext<MyMessage> context)
        {
            await Console.Out.WriteLineAsync($"Command says: {context.Message.MessageText}");

            // update the customer address

        }
    }
}