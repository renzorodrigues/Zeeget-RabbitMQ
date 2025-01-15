namespace Zeeget_RabbitMQ.Services
{
    public class RabbitMQBackgroundServiceOptions
    {
        public string QueueName { get; set; } = string.Empty;
        public Func<string, Task> OnMessageReceived { get; set; } =
            async _ => await Task.CompletedTask;
    }
}
