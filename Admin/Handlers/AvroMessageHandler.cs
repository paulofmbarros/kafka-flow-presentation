using KafkaFlow;
using SchemaRegistry;

namespace Admin.Handlers;

public class AvroMessageHandler : IMessageHandler<AvroLogMessage>
{
    public Task Handle(IMessageContext context, AvroLogMessage message)
    {
        if (string.IsNullOrEmpty(message.Severity.ToString()))
            throw new InvalidDataException("Missing Message Text");

        Console.WriteLine(
            "Partition: {0} | Offset: {1} | WorkerId: {2} Message: {3} | Avro",
            context.ConsumerContext.Partition,
            context.ConsumerContext.Offset,
            context.ConsumerContext.WorkerId,
            message.Severity.ToString());

        return Task.CompletedTask;
    }
}