
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
await channel.QueueDeclareAsync(queue: "responses", durable: false, exclusive: false, autoDelete: false);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();

    var request = JsonSerializer.Deserialize<GenerationRequest>(Encoding.UTF8.GetString(body));

    var (id, size, quality, seed) = request!;

    Console.WriteLine($" [*] Received request! {id} \n Request: {Encoding.UTF8.GetString(body)}");

    var response = new GenerationResponse(
        Id: id,
        Result: JuliaFractal.GenerateFractal(size ?? 256, quality ?? 80, seed ?? random.Next())
    );

    await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "responses", body: JsonSerializer.SerializeToUtf8Bytes(response));
    Console.WriteLine($" [x] Sent response! {id}");
};

await channel.BasicConsumeAsync("requests", autoAck: true, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
