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

    ﻿using System;
    using System.Linq;
    using KafkaFlow;
    using KafkaFlow.Producers;
    using KafkaFlow.Sample.BatchOperations;
    using KafkaFlow.Serializer;
    using Microsoft.Extensions.DependencyInjection;


    public class Program
    {
        const string batchTestTopic = "batch-test-topic";

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
                                                    .WithWorkersCount(10)
                                                    .AddMiddlewares(
                                                        middlewares => middlewares
                                                            .AddDeserializer<NewtonsoftJsonDeserializer>()
                                                            .AddTypedHandlers(
                                                                handlers => handlers
                                                                    .AddHandler<SampleMessageHandler>())
                                                    )
                                            )
                                            // .AddConsumer(
                                            //     consumerBuilder => consumerBuilder
                                            //         .Topic(batchTestTopic)
                                            //         .WithGroupId("kafkaflow-sample")
                                            //         .WithBufferSize(10000)
                                            //         .WithWorkersCount(1)
                                            //         .AddMiddlewares(
                                            //             middlewares => middlewares
                                            //                 .AddDeserializer<>()
                                            //                 .AddBatching(10, TimeSpan.FromSeconds(10))
                                            //                 .Add<PrintConsoleMiddleware>()
                                            //         )
                                            // )


                                    )
                            );
                    })
                .Build()
                .Run();
        }
    }

    public class SampleMessageHandler : KafkaFlow.TypedHandler.IMessageHandler<SampleMessage>
    {
        public async Task Handle(IMessageContext context, SampleMessage message)
        {
            await Task.Delay(1000);

            Console.WriteLine("Message read form consumer: " + context.ConsumerContext.ConsumerName);
            Console.WriteLine("Message number: " + message.MessageNumber);
            Console.WriteLine("Message read from worker: " + context.ConsumerContext.WorkerId);

            Console.WriteLine("Processed: " + message.Value);
        }
    }

    internal class PrintConsoleMiddleware : IMessageMiddleware
    {
        public Task Invoke(IMessageContext context, MiddlewareDelegate next)
        {
            var batch = context.GetMessagesBatch();

            var text = string.Join(
                '\n',
                batch.Select(ctx => ((SampleBatchMessage)ctx.Message.Value).Text));

            Console.WriteLine(text);

            return Task.CompletedTask;
        }
    }
}