using ConsumerWithOTEL.Common;
using KafkaFlow;
using Microsoft.Extensions.Logging;
using SchemaRegistry;

namespace ConsumerWithOTEL.Handlers;

public class PrintConsoleHandler(ILogger<PrintConsoleHandler> logger) : IMessageHandler<AvroLogMessage>
{

    public Task Handle(IMessageContext context, AvroLogMessage message)
    {
        using var activity = TestTraces.StartActivity("Handler");
        if (string.IsNullOrEmpty(message.Severity.ToString()))
            throw new InvalidDataException("Missing Message Text");

        logger.LogInformation(
            "Partition: {partition} | Offset: {offset} | WorkerId: {WorkerId} Message: {Severity} | Avro",
            context.ConsumerContext.Partition,
            context.ConsumerContext.Offset,
            context.ConsumerContext.WorkerId,
            message.Severity.ToString());

        return Task.CompletedTask;
    }
}