using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.OpenTelemetry;
using KafkaFlow.Producers;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SchemaRegistry;

namespace SimpleProducerApp;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        const string avroTopic = "avro-topic";
        const string avroProducerName = "kafka-flow-producer";



        // Setting up OpenTelemetry TracerProvider in the Producer
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                serviceName: "ProducerApp", // Name of the producer service
                serviceVersion: "1.0.0"))
            .AddSource(KafkaFlowInstrumentation.ActivitySourceName)
            .AddConsoleExporter()
            .AddAspNetCoreInstrumentation()
            // .AddJaegerExporter(options =>
            //     {
            //         options.AgentHost = "localhost"; // Jaeger's agent
            //         options.AgentPort = 6831;
            //         options.ExportProcessorType = ExportProcessorType.Simple;
            //     }
            //
            // ) // Jaeger's OTLP receiver
            //.AddOtlpExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317"); // Jaeger's OTLP receiver
                options.Protocol = OtlpExportProtocol.Grpc;
            })
            // .AddOtlpExporter(options =>
            // {
            //     options.Endpoint = new Uri("http://localhost:14250"); // Jaeger's OTLP receiver
            // })
            .Build();

        
        services.AddKafka(
            kafka => kafka
                .UseConsoleLog()
                .AddOpenTelemetryInstrumentation(options =>
                {
                    options.EnrichProducer = (activity, messageContext) =>
                    {
                        activity.SetTag("messaging.destination.producername", "KafkaFlowOtel");
                    };
                })
                .AddCluster(
                    cluster => cluster
                        .WithBrokers(new[] { "localhost:9092" })
                        .WithSchemaRegistry(config => config.Url = "localhost:8081")
                        .CreateTopicIfNotExists(avroTopic, 1, 1)

                        .AddProducer(
                            avroProducerName,
                            producer => producer
                                .DefaultTopic(avroTopic)
                                .AddMiddlewares(
                                    middlewares => middlewares
                                        .AddSchemaRegistryAvroSerializer(
                                            new AvroSerializerConfig
                                            {
                                                SubjectNameStrategy = SubjectNameStrategy.TopicRecord
                                            })
                                )
                                .WithAcks(Acks.All))

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


            // //batchProducer
            // await BatchProduce(producer, count, avroTopic);

            for (var i = 0; i < count; i++)
            {
                try
                {
                    await SimpleProduce(producer, avroTopic);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

    }

    private static async Task SimpleProduce(IMessageProducer producer, string avroTopic)
    {
        await producer.ProduceAsync(avroTopic,Guid.NewGuid().ToString(), new AvroLogMessage
        {
            Severity = LogLevel.Info
        });
    }

    private static async Task BatchProduce(IMessageProducer producer, int count, string avroTopic)
    {
        await producer
            .BatchProduceAsync(
                Enumerable
                    .Range(0, count)
                    .Select(
                        _ => new BatchProduceItem(
                            avroTopic,
                            Guid.NewGuid().ToString(),
                            new AvroLogMessage
                            {
                                Severity = LogLevel.Info
                            },
                            null))
                    .ToList());
    }
}