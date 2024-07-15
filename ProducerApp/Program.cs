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

                if (!int.TryParse(input, out var count))
                    continue;

                for (var i = 0; i < count; i++)
                {
                    var message = new SampleMessage
                    {
                        Id = Guid.NewGuid(),
                        Value = "Message Value - " + Guid.NewGuid(),
                        MessageNumber = i
                    };

                   await producer.ProduceAsync(message.Id.ToString(), message);
                }
            }
        }
    }
}