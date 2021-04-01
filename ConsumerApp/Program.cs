namespace ConsumerApp
{
    using System;
    using System.Threading.Tasks;
    using Common;
    using KafkaFlow;
    using KafkaFlow.Serializer;
    using KafkaFlow.Serializer.NewtonsoftJson;
    using KafkaFlow.TypedHandler;
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static void Main(string[] args)
        {
            Host
                .CreateDefaultBuilder(args)
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services
                            .AddKafkaFlowHostedService(
                                kafka => kafka
                                    .UseConsoleLog()
                                    .AddCluster(
                                        cluster => cluster
                                            .WithBrokers(new[] { "localhost:9092" })
                                            .AddConsumer(
                                                consumer => consumer
                                                    .Topic("presentation-topic")
                                                    .WithGroupId("print-console-handler")
                                                    .WithBufferSize(100)
                                                    .WithWorkersCount(1)
                                                    .AddMiddlewares(
                                                        middlewares => middlewares
                                                            .AddSerializer<NewtonsoftJsonMessageSerializer>()
                                                            .AddTypedHandlers(
                                                                handlers => handlers
                                                                    .WithHandlerLifetime(InstanceLifetime.Singleton)
                                                                    .AddHandler<SampleMessageHandler>())
                                                    )
                                            )
                                    )
                            );
                    })
                .Build()
                .Run();
        }
    }

    public class SampleMessageHandler : IMessageHandler<SampleMessage>
    {
        public async Task Handle(IMessageContext context, SampleMessage message)
        {
            await Task.Delay(1000);

            Console.WriteLine("Processed: " + message.Value);
        }
    }
}
