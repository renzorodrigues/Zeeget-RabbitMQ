namespace Zeeget_RabbitMQ.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message, string exchange, string routingKey);
    }
}
