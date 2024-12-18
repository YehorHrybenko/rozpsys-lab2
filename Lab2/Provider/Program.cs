﻿
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static Models.GenerationModel;
using static Provider.Algorithm.FractalGenerator;

var random = new Random();

Func<string, string?> env = Environment.GetEnvironmentVariable;

var factory = new ConnectionFactory
{
    HostName = env("RMQ_HOST")!,
    Port = int.Parse(env("RMQ_PORT")!),
    UserName = env("RMQ_USERNAME")!,
    Password = env("RMQ_PASSWORD")!,
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "requests", durable: false, exclusive: false, autoDelete: false);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();

    var request = JsonSerializer.Deserialize<GenerationRequest>(Encoding.UTF8.GetString(body));

    var (id, size, quality, seed, replyTo) = request!;

    Console.WriteLine($" [*] Received request {id}!");

    Stopwatch sw = Stopwatch.StartNew();

    string fractal = "";

    if (!CheckIfTimeTest(request))
    {
        fractal = JuliaFractal.GenerateFractal(size ?? 256, quality ?? 80, seed ?? random.Next());
    }

    sw.Stop();

    var response = new GenerationResponse(
        Id: id,
        Result: fractal

    );

    sw.Stop();

    await channel.BasicPublishAsync(exchange: string.Empty, routingKey: replyTo, body: JsonSerializer.SerializeToUtf8Bytes(response));

    Console.WriteLine($" [*] Calculations time: {sw.ElapsedMilliseconds} ms.");

    Console.WriteLine($" [x] Sent response {id}!");
};

static bool CheckIfTimeTest(GenerationRequest request) => request.Seed == -2;

await channel.BasicConsumeAsync("requests", autoAck: true, consumer: consumer);

Console.ReadLine();
