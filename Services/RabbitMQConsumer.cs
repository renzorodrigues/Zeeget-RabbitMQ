using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zeeget_RabbitMQ.Interfaces;
using Zeeget_RabbitMQ.Models;

namespace Zeeget_RabbitMQ.Services
{
    public class RabbitMQConsumer : IMessageConsumer
    {
        private readonly ConnectionFactory _factory;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private readonly string _modulePrefix;

        public RabbitMQConsumer(
            RabbitMQSettings settings,
            ILogger<RabbitMQConsumer> logger,
            string modulePrefix
        )
        {
            _logger = logger;
            _modulePrefix = modulePrefix;
            _factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost
            };
        }

        public async Task ConsumeAsync<TEvent>(string queueName, Func<TEvent, Task> onMessageReceived)
        {
            var connection = await _factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            var prefixedQueueName = $"{_modulePrefix}.{queueName}";
            await channel.QueueDeclareAsync(
                prefixedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageString = Encoding.UTF8.GetString(body);

                    _logger.LogInformation(
                        "Message received from queue {QueueName}.",
                        prefixedQueueName
                    );

                    // Deserialize the message
                    var message = JsonSerializer.Deserialize<TEvent>(messageString);

                    if (message is not null)
                    {
                        await onMessageReceived(message);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Message from queue {QueueName} could not be deserialized.",
                            prefixedQueueName
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing message from queue {QueueName}.",
                        prefixedQueueName
                    );
                }
            };

            await channel.BasicConsumeAsync(prefixedQueueName, autoAck: true, consumer);
        }
    }
}
