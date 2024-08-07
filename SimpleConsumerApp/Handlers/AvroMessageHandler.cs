﻿using KafkaFlow;
using SchemaRegistry;

namespace SimpleConsumerApp.Handlers;

public class AvroMessageHandler : IMessageHandler<AvroLogMessage>
{
    public Task Handle(IMessageContext context, AvroLogMessage message)
    {
        if (string.IsNullOrEmpty(message.Severity.ToString()))
            throw new InvalidDataException("Missing Message Text");

        Console.WriteLine(
            "Partition: {0} | Offset: {1} | Message: {2} | Avro",
            context.ConsumerContext.Partition,
            context.ConsumerContext.Offset,
            message.Severity.ToString());

        return Task.CompletedTask;
    }
}