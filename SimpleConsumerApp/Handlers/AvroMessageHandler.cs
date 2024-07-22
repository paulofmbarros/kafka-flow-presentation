using KafkaFlow;
using SchemaRegistry;

namespace SimpleConsumerApp.Handlers;

public class AvroMessageHandler : IMessageHandler<AvroLogMessage>
{
    public Task Handle(IMessageContext context, AvroLogMessage message)
    {
        Console.WriteLine(
            "Partition: {0} | Offset: {1} | Message: {2} | Avro",
            context.ConsumerContext.Partition,
            context.ConsumerContext.Offset,
            message.Message);

        return Task.CompletedTask;
    }
}