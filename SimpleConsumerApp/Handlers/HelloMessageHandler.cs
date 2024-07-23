using KafkaFlow;
using SchemaRegistry;

namespace SimpleConsumerApp.Handlers;

public class HelloMessageHandler : IMessageHandler<AvroLogMessage>
{
    private static readonly Random Random = new(Guid.NewGuid().GetHashCode());
    private static bool ShouldFail() => Random.Next(2) == 1;

    public Task Handle(IMessageContext context, AvroLogMessage message)
    {
        if (ShouldFail())
        {
            Console.WriteLine(
                "Let's fail: Partition: {0} | Offset: {1} | Message: {2}",
                context.ConsumerContext.Partition,
                context.ConsumerContext.Offset,
                message.Severity.ToString());
            throw new IOException();
        }

        Console.WriteLine(
            "Partition: {0} | Offset: {1} | Message: {2}",
            context.ConsumerContext.Partition,
            context.ConsumerContext.Offset,
            message.Severity.ToString());

        return Task.CompletedTask;
    }
}