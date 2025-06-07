using System;
using MassTransit;
using System.Threading.Tasks;
using CTM.EnterpriseBus.Contracts.Messages;

namespace CTM.Message.Processors
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