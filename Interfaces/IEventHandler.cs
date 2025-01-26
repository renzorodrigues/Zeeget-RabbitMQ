namespace Zeeget_RabbitMQ.Interfaces
{
    public interface IEventHandler<TEvent>
    {
        string QueueName { get; }
        Task HandleMessageAsync(TEvent message);
    }
}
