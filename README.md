# Zeeget-RabbitMQ: RabbitMQ Library for .NET 8

Zeeget-RabbitMQ simplifies the integration with RabbitMQ in .NET 8 applications. It provides a streamlined API for publishing and consuming messages, automates the registration of `BackgroundServices`, and supports dependency injection for easy configuration.

## Features

- **Automatic BackgroundService Registration**: Automatically sets up consumers for each `IEventHandler` implementation.
- **Publisher and Consumer Abstractions**: Easy-to-use interfaces (`IMessagePublisher` and `IMessageConsumer`).
- **Dependency Injection**: Quick setup using the `AddRabbitMQ` extension method.
- **Retry Policies**: Built-in support for customizable retry mechanisms.
- **Dynamic Handler Registration**: Automatically discovers and registers event handlers at runtime.

---

## Installation

Add the package from NuGet:
```bash
Install-Package Zeeget-RabbitMQ
```

---

## Usage

### 1. **Setup Dependency Injection**

In your `Program.cs`, configure RabbitMQ settings and register the library services:

```csharp
using Zeeget_RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Configure RabbitMQ
builder.Services.AddRabbitMQ("MyModule", options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.VirtualHost = "/";
    options.RetryCount = 3;
    options.RetryInterval = 2;
});

// Add other services
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
```

---

### 2. **Publish Messages**

Inject `IMessagePublisher` into your controllers or services to publish messages:

```csharp
using Zeeget_RabbitMQ.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class PublishController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public PublishController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("publish")]
    public async Task<IActionResult> PublishMessage([FromBody] string message)
    {
        await _publisher.PublishAsync(message, "my_exchange", "my_routing_key");
        return Ok("Message published successfully.");
    }
}
```

---

### 3. **Consume Messages**

Create an `IEventHandler` implementation for each queue you want to consume from. The library will automatically discover and register these handlers:

```csharp
using Zeeget_RabbitMQ.Interfaces;

public class MyEventHandler : IEventHandler
{
    public string QueueName => "my_queue";

    public async Task HandleMessageAsync(string message)
    {
        Console.WriteLine($"Message received from {QueueName}: {message}");
        await Task.CompletedTask;
    }
}
```

The library will automatically configure `BackgroundServices` to consume messages using the event handlers.

---

## Configuration Options

The following properties can be configured through `RabbitMQSettings`:

| Property       | Description                                   | Default     |
|----------------|-----------------------------------------------|-------------|
| `HostName`     | The hostname of the RabbitMQ server          | `localhost` |
| `Port`         | The port for RabbitMQ                        | `5672`      |
| `UserName`     | Username for authentication                  | `guest`     |
| `Password`     | Password for authentication                  | `guest`     |
| `VirtualHost`  | Virtual host for RabbitMQ                    | `/`         |
| `RetryCount`   | Number of retry attempts for failed messages | `3`         |
| `RetryInterval`| Interval (seconds) between retry attempts    | `2`         |

---

## Key Changes in Version 2.0.0

- **Automatic BackgroundService Configuration**: Consumers no longer need to manually configure `BackgroundServices`. The library handles this automatically by discovering `IEventHandler` implementations.
- **Simplified API**: The library streamlines setup, reducing boilerplate code.

---

## Example Workflow

1. **Publish a Message**:
   - Use `IMessagePublisher` to send a message to an exchange with a routing key.

2. **Consume a Message**:
   - Implement `IEventHandler` for each queue to define custom message handling logic.

3. **Error Handling**:
   - The library retries failed messages based on the configured retry policy.

---

## Contributing

Feel free to open issues or contribute by submitting pull requests.

---

## License

This project is licensed under the [MIT License](LICENSE).