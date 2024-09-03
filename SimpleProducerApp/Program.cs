using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.OpenTelemetry;
using KafkaFlow.Producers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SchemaRegistry;
using LogLevel = SchemaRegistry.LogLevel;

namespace SimpleProducerApp;


class Program
{
    static async Task Main(string[] args)
    {
        // Set up and run the Host
        await Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => logging.AddOpenTelemetry(o =>
            {
                var resourceBuilder = ResourceBuilder.CreateDefault();
                resourceBuilder.AddService(
                    serviceName: "ProducerApp", // Name of the consumer service
                    serviceVersion: "1.0.0");
                o.SetResourceBuilder(resourceBuilder);
                o.IncludeScopes = true;
                o.IncludeFormattedMessage = true;
                o.ParseStateValues = true;

                o.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri("http://localhost:4317");
                        options.Protocol = OtlpExportProtocol.Grpc;
                    }
                );
            }))
            .ConfigureServices(async (hostContext, services) =>
            {
                const string avroTopic = "avro-topic";
                const string avroProducerName = "kafka-flow-producer";

                services.AddLogging();
                // Configure OpenTelemetry Tracing
                services.AddOpenTelemetry().ConfigureResource(options => options.AddService(
                        serviceName: "ProducerApp", // Name of the consumer service
                        serviceVersion: "1.0.0"
                    ))
                    .WithTracing(builder =>
                    {
                        builder
                            .AddSource(KafkaFlowInstrumentation.ActivitySourceName) // Add KafkaFlow instrumentation
                            .AddConsoleExporter() // Optional: Add Console exporter for debugging
                            .AddAspNetCoreInstrumentation()
                            .AddOtlpExporter(options =>
                            {
                                options.Endpoint = new Uri("http://localhost:4317"); // Endpoint of OTLP receiver
                                options.Protocol = OtlpExportProtocol.Grpc;
                                options.BatchExportProcessorOptions = new()
                                {
                                    MaxQueueSize = 1000,
                                    ScheduledDelayMilliseconds = 1000,
                                    MaxExportBatchSize = 100
                                };
                            });
                    });

                // Configure Kafka Producer
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
                                .WithSchemaRegistry(config => config.Url = "http://localhost:8081")
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
            })
            .Build()
            .RunAsync();
    }

    private static async Task SimpleProduce(IMessageProducer producer, string avroTopic)
    {
        await producer.ProduceAsync(avroTopic, Guid.NewGuid().ToString(), new AvroLogMessage
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