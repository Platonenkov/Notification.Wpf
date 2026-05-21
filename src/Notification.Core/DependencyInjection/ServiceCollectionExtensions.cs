using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Notification.Core
{
    /// <summary>
    /// Provides extension methods for registering core notification services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class NotificationCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the core notification services including configuration, event service, and notification queue.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="configure">An optional action to customize the notification configuration.</param>
        /// <returns>The service collection for chaining.</returns>
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
