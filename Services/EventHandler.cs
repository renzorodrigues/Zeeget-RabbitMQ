using Zeeget_RabbitMQ.Interfaces;

namespace Zeeget_RabbitMQ.Services
{
    public class EventHandler : IEventHandler
    {
        public string QueueName => "my_queue";

        public async Task HandleMessageAsync(string message)
        {
            Console.WriteLine($"Processing message from {QueueName}: {message}");
            await Task.CompletedTask;
        }
    }
}
