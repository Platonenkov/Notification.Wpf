using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notification.Core;

namespace Notification.Console
{
    public static class ConsoleNotificationServiceCollectionExtensions
    {
        public static IServiceCollection AddConsoleNotifications(
            this IServiceCollection services,
            Action<INotificationConfiguration> configure = null)
        {
            services.AddNotifications(configure);

            services.TryAddSingleton<ConsoleNotificationManager>(sp =>
            {
                INotificationConfiguration config = sp.GetRequiredService<INotificationConfiguration>();
                INotificationEventService events = sp.GetRequiredService<INotificationEventService>();
                return new ConsoleNotificationManager(config, events);
            });

            services.TryAddSingleton<INotificationService>(sp => sp.GetRequiredService<ConsoleNotificationManager>());

            return services;
        }
    }
}
