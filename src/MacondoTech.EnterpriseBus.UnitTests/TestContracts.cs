using MacondoTech.EnterpriseBus.Conventions;
using MacondoTech.EnterpriseBus.UnitTests.Events;
using MacondoTech.EnterpriseBus.UnitTests.Messages;
using MacondoTech.EnterpriseBus.UnitTests.Requests;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MacondoTech.EnterpriseBus.UnitTests.Events
{
    [UseMessageStorage]
    public interface Test1Event
    {
        int MyProperty { get; set; }
    }
    public interface Test2Event
    {
        string MyProperty { get; set; }
    }
}

namespace MacondoTech.EnterpriseBus.UnitTests.Requests
{
    public interface Test1Request
    {
        int MyProperty { get; set; }
    }
    public interface Test2Request
    {
        string MyProperty { get; set; }
    }
}
namespace MacondoTech.EnterpriseBus.UnitTests.Messages
{
    public class Test1Message
    {
        public int MyProperty { get; set; }
    }
}
namespace MacondoTech.EnterpriseBus.UnitTests.Consumers
{
    public class Test1EventProcessor : IConsumer<Test1Event>
    {
        public async Task Consume(ConsumeContext<Test1Event> context)
        {
            await Console.Out.WriteLineAsync("done!");
            // update the customer address

        }
    }
    public class Test1EventProcessor2 : IConsumer<Test1Event>
    {
        public async Task Consume(ConsumeContext<Test1Event> context)
        {
            await Console.Out.WriteLineAsync("done!");
            // update the customer address

        }
    }
    public class Test2EventProcessor : IConsumer<Test2Event>
    {
        public async Task Consume(ConsumeContext<Test2Event> context)
        {
            await Console.Out.WriteLineAsync("done!");
            // update the customer address

        }
    }
    public class Test1RequestProcessor : IConsumer<Test1Request>
    {
        public async Task Consume(ConsumeContext<Test1Request> context)
        {
            await Console.Out.WriteLineAsync("done!");
            // update the customer address

        }
    }
    public class Test2RequestProcessor : IConsumer<Test2Request>
    {
        public async Task Consume(ConsumeContext<Test2Request> context)
        {
            await Console.Out.WriteLineAsync("done!");
            // update the customer address

        }
    }
    public class Test1MessageProcessor : IConsumer<Test1Message>
    {
        public async Task Consume(ConsumeContext<Test1Message> context)
        {
            await Console.Out.WriteLineAsync("done!");
            // update the customer address
        }
    }
}
