// See https://aka.ms/new-console-template for more information

using ConsumerWithOTEL;
using ConsumerWithOTEL.Common;
using ConsumerWithOTEL.Handlers;
using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public class Program
{

    public async static Task Main(string[] args)
    {
        var services = new ServiceCollection();

        const string producerName = "PrintConsole";
        const string topicName =  "avro-topic";


        // using var tracerProvider = Sdk.CreateTracerProviderBuilder()
        //     .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
        //         serviceName: "ConsumerApp", // Name of the consumer service
        //         serviceVersion: "1.0.0"))
        //     .AddSource(KafkaFlowInstrumentation.ActivitySourceName)
        //     .AddConsoleExporter()
        //     .AddAspNetCoreInstrumentation()
        //     // .AddJaegerExporter(options => options.Endpoint = new Uri("http://localhost:14250")) // Jaeger's OTLP receiver
        //     // .AddOtlpExporter()
        //     .AddOtlpExporter(options =>
        //     {
        //         options.Endpoint = new Uri("http://localhost:4317"); // Jaeger's OTLP receiver
        //         options.Protocol = OtlpExportProtocol.Grpc;
        //     })
        //     .Build();


      await Host
            .CreateDefaultBuilder(args)
            .ConfigureLogging(logging=> logging.AddOpenTelemetry(o =>
                {
                    var resourceBuilder = ResourceBuilder.CreateDefault();
                    resourceBuilder.AddService(
                        serviceName: "ConsumerApp", // Name of the consumer service
                        serviceVersion: "1.0.0");
                    o.SetResourceBuilder(resourceBuilder);
                    o.IncludeScopes = true;
                    o.IncludeFormattedMessage = true;
                    o.ParseStateValues = true;

                    o.AddOtlpExporter(options =>
                        {
                            options.Endpoint =new Uri("http://localhost:4317");
                            options.Protocol = OtlpExportProtocol.Grpc;
                        }
                    );
                }))
                .ConfigureServices(
                (hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddOpenTelemetry().ConfigureResource(options => options.AddService(
                            serviceName: "ConsumerApp", // Name of the consumer service
                            serviceVersion: "1.0.0"
                        ))
                        .WithTracing(
                            options =>
                                options.AddSource(KafkaFlowInstrumentation.ActivitySourceName)
                                    .AddConsoleExporter()
                                    .AddAspNetCoreInstrumentation()
                                    .AddOtlpExporter(
                                        options =>
                                        {
                                            options.Endpoint = new Uri("http://localhost:4317"); // Jaeger's OTLP receiver
                                            options.Protocol = OtlpExportProtocol.Grpc;
                                            options.BatchExportProcessorOptions = new()
                                            {
                                                MaxQueueSize = 1000,
                                                ScheduledDelayMilliseconds = 1000,
                                                MaxExportBatchSize = 100
                                            };
                                        }
                                            )
                                    .AddSource(TestTraces.ActivitySourceName)
                        );

                    services.AddKafkaFlowHostedService(
                        kafka => kafka
                            .UseConsoleLog()
                            .AddOpenTelemetryInstrumentation(options =>
                            {
                                options.EnrichConsumer = (activity, messageContext) =>
                                {
                                    activity.SetTag("messaging.destination.groupid",
                                        messageContext.ConsumerContext.GroupId);
                                };
                            })
                            .AddCluster(
                                cluster => cluster
                                    .WithBrokers(new[]
                                    {
                                        "localhost:9092"
                                    })
                                    .WithSchemaRegistry(config => config.Url = "localhost:8081")
                                    .AddConsumer(
                                        consumer => consumer
                                            .Topic(topicName)
                                            .WithGroupId("avro-group-id")
                                            .WithBufferSize(100)
                                            .WithWorkersCount(4)
                                            .AddMiddlewares(
                                                middlewares => middlewares
                                                    .AddSchemaRegistryAvroDeserializer()
                                                    .AddTypedHandlers(h => h.AddHandler<PrintConsoleHandler>())
                                            )
                                    )
                            )
                    );
                })

            .Build()
            .RunAsync();

    }
}