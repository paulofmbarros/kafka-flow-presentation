// See https://aka.ms/new-console-template for more information

using KafkaFlow;
using KafkaFlow.Retry;
using Microsoft.Extensions.Hosting;
using SimpleConsumerApp.Handlers;
using SimpleConsumerApp.Middleware;

namespace SimpleConsumerApp;

public class Program
{
    const string avroTopic = "avro-topic";

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
                                        .WithSchemaRegistry(config => config.Url = "localhost:8081")
                                        .AddConsumer(
                                            consumer => consumer
                                                .Topic(avroTopic)
                                                .WithGroupId("avro-group-id")
                                                .WithBufferSize(100)
                                                .WithWorkersCount(1)
                                                .WithAutoOffsetReset(AutoOffsetReset.Latest)
                                                .AddMiddlewares(
                                                    middlewares => middlewares
                                                        //Avro Schema deserializer
                                                        .AddSchemaRegistryAvroDeserializer()
                                                        //Batching
                                                        .AddBatching(10, TimeSpan.FromSeconds(10))
                                                        .Add<PrintConsoleMiddleware>()
                                                        //Custom Middleware
                                                        .Add<PauseConsumerOnExceptionMiddleware>()
                                                        ////Policy for retrying
                                                        .RetrySimple((config)=>
                                                            config.Handle<IOException>()
                                                                .TryTimes(3)
                                                                .WithTimeBetweenTriesPlan((retryCount) => TimeSpan.FromSeconds(Math.Pow(2, retryCount) *10)))

                                                        //Handlers
                                                        .AddTypedHandlers(
                                                            handlers => handlers
                                                                // .AddHandler<HelloMessageHandler>() //Throws exception
                                                                .AddHandler<AvroMessageHandler>())
                                                )
                                        )
                                )

                        );
                })
            .Build()
            .Run();
    }
}