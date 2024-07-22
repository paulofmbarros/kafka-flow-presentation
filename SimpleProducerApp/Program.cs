


using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaFlow.Configuration;
using KafkaFlow.Middlewares.Serializer;
using KafkaFlow.Middlewares.Serializer.Resolvers;
using KafkaFlow.Producers;
using KafkaFlow.Serializer.SchemaRegistry;


namespace ProducerApp;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using KafkaFlow;
using SchemaRegistry;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // const string producerName = "sample-producer";
        const string avroTopic = "avro-topic";
        const string avroProducerName = "avro-producer";


        services.AddKafka(
            kafka => kafka
                .UseConsoleLog()

                .AddCluster(
                    cluster => cluster
                        .WithBrokers(new[] { "localhost:9092" })
                        .WithSchemaRegistry(config => config.Url = "localhost:8081")
                        .CreateTopicIfNotExists(avroTopic, 1, 1)
                        .AddProducer(
                            avroProducerName,
                            producer => producer
                                .AddMiddlewares(
                                    middlewares => middlewares
                                        .AddSchemaRegistryAvroSerializer(
                                            new AvroSerializerConfig
                                            {
                                                SubjectNameStrategy = SubjectNameStrategy.Topic
                                            }))
                        )


                        // .AddProducer(
                        //     producerName,
                        //     producer => producer
                        //         .DefaultTopic("presentation-topic")
                        //         .AddMiddlewares(
                        //             middlewares => middlewares
                        //                 .AddSerializer<NewtonsoftJsonMessageSerializer>()
                        //         )
                        // )
                )
        );

        var provider = services.BuildServiceProvider();

        var bus = provider.CreateKafkaBus();
        await bus.StartAsync();
        var producers = provider.GetRequiredService<IProducerAccessor>();
        var producer = producers[avroProducerName];

        while (true)
        {
            Console.WriteLine("Message count: ");
            var input = Console.ReadLine();

            if (input == "exit")
                return;

            if (!int.TryParse(input, out var count))
                continue;

            for (var i = 0; i < count; i++)
            {
                try
                {
                    await producer.ProduceAsync(avroTopic,Guid.NewGuid().ToString(), new AvroLogMessage
                    {
                        Message = "Simple Message"
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

}