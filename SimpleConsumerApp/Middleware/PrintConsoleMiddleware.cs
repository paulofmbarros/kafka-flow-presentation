using Common;
using KafkaFlow;
using SchemaRegistry;

namespace SimpleConsumerApp.Middleware;

public class PrintConsoleMiddleware : IMessageMiddleware
{
    public Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        var batch = context.GetMessagesBatch();

        var text = string.Join(
            '\n',
            batch.Select(ctx => ((AvroLogMessage)ctx.Message.Value)));

        Console.WriteLine(text);

        return Task.CompletedTask;
    }
}