using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using static Models.GenerationModel;

using Consumer.Services;
using System.Text;
using System.Diagnostics;

var random = new Random();

var responseHandler = new ResponseHandler();

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var factory = new ConnectionFactory
{
    HostName = config["RabbitMQ:Host"]!,
    Port = config.GetValue<int>("RabbitMQ:Port"),
    UserName = config["RabbitMQ:UserName"]!,
    Password = config["RabbitMQ:Password"]!,
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "requests", durable: false, exclusive: false, autoDelete: false);
await channel.QueueDeclareAsync(queue: "responses", durable: false, exclusive: false, autoDelete: false);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    var body = ea.Body.ToArray();

    var (id, result) = JsonSerializer.Deserialize<GenerationResponse>(body)!;

    Console.WriteLine($" [x] Received response {id}!");

    responseHandler.AddResponse(id, result);

    return Task.CompletedTask;
};

await channel.BasicConsumeAsync("responses", autoAck: true, consumer: consumer);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet(
    "/generateFractal",
    async (int? size, int? quality, int? seed, byte priority = 0) => {

        Guid requestId = Guid.NewGuid();

        var request = new GenerationRequest(
            RequestId: requestId,
            Size: size,
            Quality: quality,
            Seed: seed
        );

        Console.WriteLine($" [*] Creating request {request.RequestId}!");

        Stopwatch sw = Stopwatch.StartNew();

        Task<string> response = responseHandler.PromiseRetrieve(requestId);

        await channel.BasicPublishAsync(
            exchange: string.Empty, 
            routingKey: "requests", 
            mandatory: true,
            basicProperties: new BasicProperties() { Priority = priority }, 
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))
        );

        var res = await response;
        
        sw.Stop();

        Console.WriteLine($" [i] Request took {sw.ElapsedMilliseconds} ms.");

        return res;
    }
);

app.MapGet(
    "/timeTest",
    async (int time = 0) => {

        Guid requestId = Guid.NewGuid();

        var request = new GenerationRequest(
            RequestId: requestId,
            Size: 0,
            Quality: 0,
            Seed: -2
        );

        Console.WriteLine($" [*] Creating request {request.RequestId}!");

        Stopwatch sw = Stopwatch.StartNew();

        Task<string> response = responseHandler.PromiseRetrieve(requestId);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "requests",
            mandatory: true,
            basicProperties: new BasicProperties() { Priority = 9 },
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))
        );

        var res = await response;

        sw.Stop();

        Console.WriteLine($" [i] Request took {sw.ElapsedMilliseconds} ms.");

        return res;
    }
);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
