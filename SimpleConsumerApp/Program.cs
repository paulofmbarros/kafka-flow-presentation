// See https://aka.ms/new-console-template for more information
    using Common;
    using KafkaFlow;
    using KafkaFlow.Retry;
    using KafkaFlow.Retry.MongoDb;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;
    using SchemaRegistry;
    using SimpleConsumerApp.ContractResolver;
    using SimpleConsumerApp.Exceptions;
    using SimpleConsumerApp.Handlers;
    using SimpleConsumerApp.Middleware;

    public class Program
    {
        const string avroTopic = "avro-topic";
        const string mongoDbConnectionString = "mongodb://localhost:27017";
        const string mongoDbDatabaseName = "kafka_flow_retry_durable_sample";
        const string mongoDbRetryQueueCollectionName = "RetryQueues";
        const string mongoDbRetryQueueItemCollectionName = "RetryQueueItems";
        const string mongodbRetryTopic = "sample-kafka-flow-retry-durable-mongodb-avro-topic";

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
                                            .CreateTopicIfNotExists(avroTopic, 1, 1)
                                            .CreateTopicIfNotExists(mongodbRetryTopic, 1, 1)
                                            .CreateTopicIfNotExists("sample-kafka-flow-retry-durable-mongodb-avro-topic-retry", 1,1)
                                            .AddConsumer(
                                                    consumer => consumer
                                                        .Topic(mongodbRetryTopic)
                                                        .WithGroupId("sample-consumer-kafka-flow-retry-durable-mongodb-avro")
                                                        .WithName("kafka-flow-retry-durable-mongodb-avro-consumer")
                                                        .WithBufferSize(10)
                                                        .WithWorkersCount(1)
                                                        .WithAutoOffsetReset(AutoOffsetReset.Latest)
                                                        .AddMiddlewares(
                                                            middlewares => middlewares
                                                                .AddSchemaRegistryAvroDeserializer()
                                                                .RetryDurable(
                                                                    configure => configure
                                                                        .Handle<RetryDurableTestException>()
                                                                        .WithMessageType(typeof(AvroLogMessage))
                                                                        .WithMessageSerializeSettings(new JsonSerializerSettings
                                                                        {
                                                                            ContractResolver = new WritablePropertiesOnlyResolver()
                                                                        })
                                                                        .WithMongoDbDataProvider(
                                                                            mongoDbConnectionString,
                                                                            mongoDbDatabaseName,
                                                                            mongoDbRetryQueueCollectionName,
                                                                            mongoDbRetryQueueItemCollectionName)
                                                                        //make it simple retry before make it durable
                                                                        .WithRetryPlanBeforeRetryDurable(
                                                                            configure => configure
                                                                                .TryTimes(3)
                                                                                .WithTimeBetweenTriesPlan(
                                                                                    TimeSpan.FromMilliseconds(250),
                                                                                    TimeSpan.FromMilliseconds(500),
                                                                                    TimeSpan.FromMilliseconds(1000))
                                                                                .ShouldPauseConsumer(false)
                                                                        )
                                                                        .WithEmbeddedRetryCluster(
                                                                            cluster,
                                                                            configure => configure
                                                                                .WithRetryTopicName(
                                                                                    "sample-kafka-flow-retry-durable-mongodb-avro-topic-retry")
                                                                                .WithRetryConsumerBufferSize(4)
                                                                                .WithRetryConsumerWorkersCount(1)
                                                                                .WithRetryConsumerStrategy(
                                                                                    RetryConsumerStrategy.GuaranteeOrderedConsumption)
                                                                                .WithRetryTypedHandlers(
                                                                                    handlers => handlers
                                                                                        .WithHandlerLifetime(InstanceLifetime.Transient)
                                                                                        .AddHandler<AvroMessageThrowsExceptionHandler>()
                                                                                )
                                                                                .Enabled(true)
                                                                        )
                                                                        .WithPollingJobsConfiguration(
                                                                            configure => configure
                                                                                .WithSchedulerId("retry-durable-mongodb-polling-id")
                                                                                .WithRetryDurablePollingConfiguration(
                                                                                    configure => configure
                                                                                        .WithCronExpression("0 0/1 * 1/1 * ? *")
                                                                                        .WithExpirationIntervalFactor(1)
                                                                                        .WithFetchSize(10)
                                                                                        .Enabled(true)
                                                                                )
                                                                                .WithCleanupPollingConfiguration(
                                                                                    configure => configure
                                                                                        .WithCronExpression("0 0 * 1/1 * ? *")
                                                                                        .WithRowsPerRequest(1048)
                                                                                        .WithTimeToLiveInDays(60)
                                                                                        .Enabled(true)
                                                                                )
                                                                        ))
                                                                .AddTypedHandlers(
                                                                    handlers => handlers
                                                                        .WithHandlerLifetime(InstanceLifetime.Transient)
                                                                        .AddHandler<AvroMessageThrowsExceptionHandler>())
                    )


                                            // .AddConsumer(
                                            //     consumer => consumer
                                            //         .Topic(avroTopic)
                                            //         .WithGroupId("avro-group-id")
                                            //         .WithBufferSize(100)
                                            //         .WithWorkersCount(1)
                                            //         .WithAutoOffsetReset(AutoOffsetReset.Latest)
                                            //         .AddMiddlewares(
                                            //             middlewares => middlewares
                                            //                 //Avro Schema deserializer
                                            //                 .AddSchemaRegistryAvroDeserializer()
                                            //                 //Batching
                                            //                 // .AddBatching(10, TimeSpan.FromSeconds(10))
                                            //                 ////Custom Middleware
                                            //                 // .Add<PauseConsumerOnExceptionMiddleware>()
                                            //                 ////Policy for retrying
                                            //                 // .RetrySimple((config)=>
                                            //                 //     config.Handle<IOException>()
                                            //                 //         .TryTimes(3)
                                            //                 //         .WithTimeBetweenTriesPlan((retryCount) => TimeSpan.FromSeconds(Math.Pow(2, retryCount) *100)))
                                            //
                                            //                 //Handlers
                                            //                 .AddTypedHandlers(
                                            //                     handlers => handlers
                                            //                         // .AddHandler<HelloMessageHandler>()
                                            //                         .AddHandler<AvroMessageHandler>())
                                            //         )
                                            // )
                                    )
                            ));
                    })
                .Build()
                .Run();
        }
    }