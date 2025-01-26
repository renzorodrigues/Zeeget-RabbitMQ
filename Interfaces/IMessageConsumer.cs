namespace Zeeget_RabbitMQ.Interfaces
{
    public interface IMessageConsumer
    {
        Task ConsumeAsync<TEvent>(string queueName, Func<TEvent, Task> onMessageReceived);
    }
}
