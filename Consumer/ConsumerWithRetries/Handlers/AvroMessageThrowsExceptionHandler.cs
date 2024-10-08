﻿using ConsumerWithRetries.Exceptions;
using KafkaFlow;
using SchemaRegistry;

namespace ConsumerWithRetries.Handlers;

public class AvroMessageThrowsExceptionHandler : IMessageHandler<AvroLogMessage>
{
    public Task Handle(IMessageContext context, AvroLogMessage message)
    {
        Console.WriteLine(
            "Partition: {0} | Offset: {1} | Message: {2} | Avro",
            context.ConsumerContext.Partition,
            context.ConsumerContext.Offset,
            message.Severity.ToString());

        throw new RetryDurableTestException($"Error: {message.Severity}");
    }
}