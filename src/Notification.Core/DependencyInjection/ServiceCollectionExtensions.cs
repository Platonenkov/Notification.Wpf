using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Notification.Core
{
    public static class NotificationCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddNotifications(
            this IServiceCollection services,
            Action<INotificationConfiguration> configure = null)
        {
            services.TryAddSingleton<INotificationConfiguration>(sp =>
            {
                NotificationConfiguration config = new NotificationConfiguration();
                configure?.Invoke(config);
                return config;
            });

            services.TryAddSingleton<INotificationEventService, NotificationEventService>();
            services.TryAddSingleton<INotificationQueue>(sp =>
                new NotificationQueue(sp.GetRequiredService<INotificationService>()));

            return services;
        }
    }
}
