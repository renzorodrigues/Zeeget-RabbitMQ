using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zeeget_RabbitMQ.Interfaces;
using Zeeget_RabbitMQ.Models;
using Zeeget_RabbitMQ.Services;

namespace Zeeget_RabbitMQ
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRabbitMQ(
            this IServiceCollection services,
            string moduleName,
            Action<RabbitMQSettings> configureSettings
        )
        {
            var settings = new RabbitMQSettings();
            configureSettings(settings);

            // Add module-specific prefix for exchanges and queues
            services.AddSingleton(settings);
            services.AddSingleton<IMessagePublisher>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<RabbitMQPublisher>>();
                return new RabbitMQPublisher(settings, logger, moduleName);
            });
            services.AddSingleton<IMessageConsumer>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<RabbitMQConsumer>>();
                return new RabbitMQConsumer(settings, logger, moduleName);
            });

            return services;
        }
    }
}
