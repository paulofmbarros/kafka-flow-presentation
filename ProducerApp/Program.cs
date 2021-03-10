namespace ProducerApp
{
    using System;
    using System.Threading.Tasks;
    using Common;
    using KafkaFlow;
    using KafkaFlow.Serializer;
    using KafkaFlow.Serializer.NewtonsoftJson;
    using Microsoft.Extensions.DependencyInjection;

    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();

            const string producerName = "sample-producer";

            services.AddKafka(
                kafka => kafka
                    .UseConsoleLog()
                    .AddCluster(
                        cluster => cluster
                            .WithBrokers(new[] { "localhost:9092" })
                            // .AddConsumer(consumer => consumer
                            //     .Topic("test-topic")
                            //     .WithGroupId("print-console-handler")
                            //     .WithBufferSize(100)
                            //     .WithWorkersCount(10)
                            //     .WithAutoOffsetReset(AutoOffsetReset.Latest)
                            //     .AddMiddlewares(middlewares => middlewares
                            //         // Install KafkaFlow.Compressor and Install KafkaFlow.Compressor.Gzip
                            //         .AddCompressor<GzipMessageCompressor>() 
                            //         // Install KafkaFlow.Serializer and Install KafkaFlow.Serializer.Protobuf
                            //         .AddSerializer<ProtobufMessageSerializer>()
                            //         // Install KafkaFlow.TypedHandler
                            //         .AddTypedHandlers(handlers => handlers
                            //             .WithHandlerLifetime(InstanceLifetime.Singleton)
                            //             .AddHandler<PrintConsoleHandler>())
                            //     )
                            // )
                            .AddProducer(
                                producerName,
                                producer => producer
                                    .DefaultTopic("presentation-topic")
                                    .AddMiddlewares(
                                        middlewares => middlewares
                                            .AddSerializer<NewtonsoftJsonMessageSerializer>()
                                    )
                            )
                    )
            );

            var provider = services.BuildServiceProvider();

            var bus = provider.CreateKafkaBus();

            var producer = bus.Producers[producerName];

            while (true)
            {
                Console.WriteLine("Message count: ");
                var input = Console.ReadLine();

                if (input == "exit")
                    return;

                if (int.TryParse(input, out var count))
                {
                    for (int i = 0; i < count; i++)
                    {
                        var message = new SampleMessage
                        {
                            Id = Guid.NewGuid(),
                            Value = "Message Value - " + Guid.NewGuid()
                        };

                        producer.Produce(message.Id.ToString(), message);
                    }
                }
            }
        }
    }
}
