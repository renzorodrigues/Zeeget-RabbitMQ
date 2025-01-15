namespace Zeeget_RabbitMQ.Interfaces
{
    public interface IEventHandler
    {
        string QueueName { get; }
        Task HandleMessageAsync(string message);
    }
}
