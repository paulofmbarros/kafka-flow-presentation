using Admin.Handlers;
using KafkaFlow;
using KafkaFlow.Admin.Dashboard;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddKafka(kafka => kafka
        .UseConsoleLog()
        .AddCluster(cluster =>
            {
                const string topicName = "topic-dashboard";
                cluster
                    .WithBrokers(new[] { "localhost:9092" })
                    .EnableAdminMessages("kafkaflow.admin", "kafkaflow.admin.group.id")
                    .EnableTelemetry("kafkaflow.admin", "kafkaflow.telemetry.group.id")
                    .CreateTopicIfNotExists(topicName, 3, 1)
                    .AddConsumer(
                        consumer =>
                        {
                            consumer
                                .Topics(topicName)
                                .WithGroupId("groupid-dashboard")
                                .WithName("consumer-dashboard")
                                .WithBufferSize(100)
                                .WithWorkersCount(20)
                                .WithAutoOffsetReset(AutoOffsetReset.Latest);
                        });
            }  )
    );

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseKafkaFlowDashboard();
}

app.UseHttpsRedirection();




var kafkaBus = app.Services.CreateKafkaBus();
await kafkaBus.StartAsync();

await app.RunAsync();



app.Run();