using System.Reflection;
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

            // Discover and register event handlers
            var handlerType = typeof(IEventHandler);
            var handlers = AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null);
                    }
                })
                .Where(type =>
                    handlerType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract
                );

            foreach (var handler in handlers)
            {
                if (handler != null)
                {
                    services.AddSingleton(handler);
                    services.AddHostedService(provider =>
                    {
                        var consumer = provider.GetRequiredService<IMessageConsumer>();
                        var eventHandler = (IEventHandler)provider.GetRequiredService(handler);

                        return new RabbitMQBackgroundService(
                            consumer,
                            eventHandler.QueueName,
                            eventHandler.HandleMessageAsync
                        );
                    });
                }                
            }

            return services;
        }
    }
}
