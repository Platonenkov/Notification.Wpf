using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notification.Core;

namespace Notification.Avalonia
{
    /// <summary>
    /// Provides extension methods for registering Avalonia notification services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class AvaloniaNotificationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Avalonia notification manager and core notification services.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="configure">An optional delegate to configure the notification settings.</param>
        /// <returns>The same service collection instance for chaining.</returns>
        public static IServiceCollection AddAvaloniaNotifications(
            this IServiceCollection services,
            Action<INotificationConfiguration> configure = null)
        {
            services.AddNotifications(configure);

            services.TryAddSingleton<AvaloniaNotificationManager>(sp =>
            {
                INotificationConfiguration config = sp.GetRequiredService<INotificationConfiguration>();
                INotificationEventService events = sp.GetRequiredService<INotificationEventService>();
                return new AvaloniaNotificationManager(config, events);
            });

            services.TryAddSingleton<INotificationService>(sp => sp.GetRequiredService<AvaloniaNotificationManager>());

            return services;
        }
    }
}
