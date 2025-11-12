using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Payments.API.Services;

public class RabbitMQService : IDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQService(IConfiguration config)
    {
        _factory = new ConnectionFactory
        {
            HostName = "26.155.73.119",
            UserName = "admin",
            Password = "admin123"
        };
    }

    private void EnsureConnected()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
        }
    }

    public void PublicarConfirmacion(object mensaje, string routingKey)
    {
        EnsureConnected();

        // Declarar la queue si no existe
        _channel!.QueueDeclare(
            queue: "pagos.dashboard",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mensaje));

        // ✅ CAMBIO CRÍTICO AQUÍ
        _channel!.BasicPublish(
            exchange: "",                    // ✅ VACÍO (sin comillas de exchange)
            routingKey: "pagos.dashboard",   // ✅ Nombre exacto de la queue
            basicProperties: null,
            body: body
        );

        Console.WriteLine($"✅ Mensaje publicado a pagos.dashboard: {JsonSerializer.Serialize(mensaje)}");
    }

    public void RecibirCompra(string queueName, Action<string> onMessageReceived)
    {
        EnsureConnected();

        // Declarar la cola si no existe
        _channel!.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            onMessageReceived(message);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}