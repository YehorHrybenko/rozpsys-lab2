using Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using static Models.GenerationModel;

using Consumer.Services;
using System.Text;

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

    responseHandler.AddResponse(id, result);
    
    Console.WriteLine($" [x] Received response for {id}!");
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

        int requestId = random.Next();

        var request = new GenerationRequest(
            RequestId: requestId,
            Size: size,
            Quality: quality,
            Seed: seed
        );

        Task<string> result = responseHandler.PromiseRetrieve(requestId);

        await channel.BasicPublishAsync(
            exchange: string.Empty, 
            routingKey: "requests", 
            mandatory: true,
            basicProperties: new BasicProperties() { Priority = priority }, 
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))
        );

        Console.WriteLine($" [*] Created request {request.RequestId}! \n Request: {JsonSerializer.Serialize(request)}");

        return await result;
    }
);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
