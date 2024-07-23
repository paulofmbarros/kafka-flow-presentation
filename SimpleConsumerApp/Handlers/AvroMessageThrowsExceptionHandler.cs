﻿using KafkaFlow;
using SchemaRegistry;
using SimpleConsumerApp.Exceptions;

namespace SimpleConsumerApp.Handlers;

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