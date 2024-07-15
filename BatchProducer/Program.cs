// See https://aka.ms/new-console-template for more information
using System;
using System.Threading.Tasks;
using Common;
using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.Serializer;
using KafkaFlow.Serializer.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;


class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        const string producerName = "sample-producer";
        const string batchTestTopic = "batch-test-topic";

        services.AddKafka(
            kafka => kafka
                .UseConsoleLog()
                .AddCluster(
                    cluster => cluster
                        .WithBrokers(new[] { "localhost:9092" })
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
            Console.Write("Number of messages to produce: ");
            var input = Console.ReadLine()!.ToLower();

            switch (input)
            {
                case var _ when int.TryParse(input, out var count):
                    await producer
                        .BatchProduceAsync(
                            Enumerable
                                .Range(0, count)
                                .Select(
                                    _ => new BatchProduceItem(
                                        batchTestTopic,
                                        Guid.NewGuid().ToString(),
                                        new SampleBatchMessage { Text = Guid.NewGuid().ToString() },
                                        null))
                                .ToList());

                    break;

                case "exit":
                    await bus.StopAsync();
                    return;
            }
        }
    }
}