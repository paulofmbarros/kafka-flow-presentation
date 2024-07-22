// See https://aka.ms/new-console-template for more information
    using Common;
    using KafkaFlow;
    using Microsoft.Extensions.Hosting;
    using SimpleConsumerApp.Handlers;
    using SimpleConsumerApp.Middleware;

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
                                                            .AddSchemaRegistryAvroDeserializer()
                                                            // .AddBatching(10, TimeSpan.FromSeconds(10))
                                                            .Add<PauseConsumerOnExceptionMiddleware>()
                                                            .AddTypedHandlers(
                                                                handlers => handlers
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