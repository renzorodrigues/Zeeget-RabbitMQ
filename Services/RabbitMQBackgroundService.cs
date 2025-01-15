using Microsoft.Extensions.Hosting;
using Zeeget_RabbitMQ.Interfaces;

namespace Zeeget_RabbitMQ.Services
{
    public class RabbitMQBackgroundService : BackgroundService
    {
        private readonly IMessageConsumer _consumer;
        private readonly string _queueName;
        private readonly Func<string, Task> _onMessageReceived;

        public RabbitMQBackgroundService(
            IMessageConsumer consumer,
            string queueName,
            Func<string, Task> onMessageReceived
        )
        {
            _consumer = consumer;
            _queueName = queueName;
            _onMessageReceived = onMessageReceived;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Listen to the specified queue
            await _consumer.ConsumeAsync(
                _queueName,
                async (message) =>
                {
                    await _onMessageReceived(message);
                }
            );

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
