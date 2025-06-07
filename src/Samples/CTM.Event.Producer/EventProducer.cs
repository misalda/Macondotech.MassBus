using MassTransit;
using System;
using System.Threading.Tasks;
using CTM.EnterpriseBus.Contracts.Messages;
using CTM.EnterpriseBus.Contracts.Events;
using CTM.EnterpriseBus.Contracts.Requests;
using CTM.EnterpriseBus.Contracts.Responses;
using CommandDotNet.Attributes;
using CTM.EnterpriseBus.Common.Services;
using MassTransit.MessageData;
using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace CTM.EventProducer
{
    public class EventProducer
    {
        [InjectProperty]
        public ICTMEnterpriseBus BusService { get; set; }
        [InjectProperty]
        public IRequestClient<SubmitOrderRequest> RequestClient { get; set; }

        [InjectProperty]

        public IMessageDataRepository MessageDataRepository { get; set; }

        [ApplicationMetadata(Description = "Publish an event for all subscribers")]
        public async Task PublishEvent(string orderDescription)
        {
            await BusService.Publish<OrderCreatedEvent>(new
            {
                OrderId = Guid.NewGuid(),
                OrderDescription = orderDescription,
                Timestamp = DateTime.Now
            });
            Console.WriteLine("Message Published!!");
        }
        [ApplicationMetadata(Description = "Publish a Big event for all subscribers using Azure Blob storage")]
        public async Task PublishBigEvent(string orderDescription)
        {
            var bytes = SerializeAsJsonByteArray(new {Something="loooong striiiiiiing" });
            await BusService.Publish<BigOrderCreatedEvent>(new
            {
                OrderId = Guid.NewGuid(),
                OrderDescription = orderDescription,
                Timestamp = DateTime.Now,
                BigData = await MessageDataRepository.PutBytes(bytes)

            });
            Console.WriteLine("Message Published!!");
        }
        [ApplicationMetadata(Description = "Sends a message to a specific endpoint configure for the message type")]
        public async Task SendMessage(string messageText)
        {
            await BusService.Send<MyMessage>(new
            {
                MessageText = messageText,
            });
            Console.WriteLine("Message Sent!!");
        }
        public async Task SendRequest(string orderDescription) {
            //var client = serviceProvider.GetService<IRequestClient<SubmitOrderRequest>>();
            var response = await RequestClient.GetResponse<SubmitOrderResponse>(new { OrderId = Guid.NewGuid() , OrderDescription= orderDescription });
            await BusService.Publish<OrderCreatedEvent>(new
            {
                OrderId = response.Message.OrderId,
                OrderDescription = response.Message.OrderDescription,
                OrderCreatedDateTime = response.Message.CreationTime,
                Timestamp = DateTime.Now
            });
        }

        private static byte[] SerializeAsJsonByteArray<T>(T item)
        {
            var stream = new MemoryStream();
            using (var streamWriter = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true)) // last parameter is important
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, item);
                streamWriter.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                return stream.ToArray();
            }
        }

    }
}
