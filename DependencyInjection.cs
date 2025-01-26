using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            // Configurar as opções de RabbitMQ
            var settings = new RabbitMQSettings();
            configureSettings(settings);

            // Registrar as dependências principais
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

            // Descobrir e registrar os Event Handlers genéricos
            var handlerType = typeof(IEventHandler<>); // Interface genérica base
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
                    type!
                        .GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                    && !type.IsInterface
                    && !type.IsAbstract
                );

            foreach (var handler in handlers)
            {
                if (handler != null)
                {
                    // Determinar o tipo genérico do handler (T)
                    var eventHandlerInterface = handler
                        .GetInterfaces()
                        .FirstOrDefault(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == handlerType
                        );
                    var messageType =
                        (eventHandlerInterface?.GetGenericArguments().FirstOrDefault())
                        ?? throw new InvalidOperationException(
                            $"Handler {handler.Name} does not implement IEventHandler<T> with a valid generic type."
                        );

                    // Registrar o handler
                    services.AddSingleton(handler);

                    // Registrar o BackgroundService dinamicamente
                    services.AddSingleton(
                        typeof(IHostedService),
                        provider =>
                        {
                            var consumer = provider.GetRequiredService<IMessageConsumer>();
                            var eventHandler = provider.GetRequiredService(handler);

                            // Obter propriedades necessárias
                            var queueNameProperty =
                                handler.GetProperty("QueueName")
                                ?? throw new InvalidOperationException(
                                    $"Handler {handler.Name} does not have a QueueName property."
                                );
                            var queueName = (string)queueNameProperty.GetValue(eventHandler)!;
                            var handleMessageAsyncMethod =
                                handler.GetMethod("HandleMessageAsync")
                                ?? throw new InvalidOperationException(
                                    $"Handler {handler.Name} does not implement HandleMessageAsync."
                                );

                            // Criar o BackgroundService dinamicamente
                            var backgroundServiceType =
                                typeof(RabbitMQBackgroundService<>).MakeGenericType(messageType);

                            return Activator.CreateInstance(
                                backgroundServiceType,
                                consumer,
                                queueName,
                                (Func<object, Task>)(
                                    message =>
                                        (Task)
                                            handleMessageAsyncMethod.Invoke(
                                                eventHandler,
                                                [message]
                                            )!
                                )
                            )!;
                        }
                    );
                }
            }

            return services;
        }
    }
}
