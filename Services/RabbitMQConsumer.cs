using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
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
        private readonly AsyncRetryPolicy _retryPolicy;

        public RabbitMQConsumer(RabbitMQSettings settings, ILogger<RabbitMQConsumer> logger)
        {
            _logger = logger;
            _factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost
            };

            // Retry policy with exponential backoff (2s, 4s, 8s, ... up to 5 attempts)
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            $"Retry {retryCount}: Failed to process RabbitMQ message. Retrying in {timeSpan.Seconds} seconds..."
                        );
                    }
                );
        }

        public async Task ConsumeAsync<TEvent>(
            string queueName,
            Func<TEvent, Task> onMessageReceived
        )
        {
            try
            {
                // Applying retry policy for RabbitMQ connection
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    var connection = await _factory.CreateConnectionAsync();
                    var channel = await connection.CreateChannelAsync();

                    await channel.QueueDeclareAsync(
                        queueName,
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
                                queueName
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
                                    queueName
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error processing message from queue {QueueName}.",
                                queueName
                            );
                        }
                    };

                    await channel.BasicConsumeAsync(queueName, autoAck: true, consumer);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RabbitMQ consumer failed to start for queue {QueueName}.",
                    queueName
                );
                throw;
            }
        }
    }
}
