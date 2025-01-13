namespace Zeeget_RabbitMQ.Interfaces
{
    public interface IMessageConsumer
    {
        Task ConsumeAsync(string queueName, Func<string, Task> onMessageReceived);
    }
}
