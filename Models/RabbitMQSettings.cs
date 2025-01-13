namespace Zeeget_RabbitMQ.Models
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public int RetryCount { get; set; } = 4; // Default retry count
        public int RetryInterval { get; set; } = 2; // Default retry interval in seconds
    }
}
