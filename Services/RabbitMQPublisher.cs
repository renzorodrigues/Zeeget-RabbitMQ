using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using Zeeget_RabbitMQ.Interfaces;
using Zeeget_RabbitMQ.Models;

namespace Zeeget_RabbitMQ.Services
{
    public class RabbitMQPublisher : IMessagePublisher
    {
        private readonly ConnectionFactory _factory;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger<RabbitMQPublisher> _logger;
        private readonly RabbitMQSettings _settings;
        private readonly string _modulePrefix;

        public RabbitMQPublisher(
            RabbitMQSettings settings,
            ILogger<RabbitMQPublisher> logger,
            string modulePrefix
        )
        {
            _settings = settings;
            _logger = logger;
            _modulePrefix = modulePrefix;
            _factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    settings.RetryCount,
                    retryAttempt =>
                    {
                        var waitTime = TimeSpan.FromSeconds(
                            Math.Pow(settings.RetryInterval, retryAttempt)
                        );
                        _logger.LogWarning(
                            "Retrying message publish in {waitTime.TotalSeconds} seconds due to exception.",
                            waitTime.TotalSeconds
                        );
                        return waitTime;
                    }
                );
        }

        public async Task PublishAsync<T>(T message, string exchange, string routingKey)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                var prefixedRoutingKey = $"{_modulePrefix}.{routingKey}";
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                var properties = new BasicProperties
                {
                    Headers = new Dictionary<string, object?> { { "ModuleName", _modulePrefix } }
                };

                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, true);
                await channel.BasicPublishAsync(
                    exchange,
                    prefixedRoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Message published successfully to exchange {Exchange} with routing key {RoutingKey}.",
                    exchange,
                    prefixedRoutingKey
                );
            });
        }
    }
}
