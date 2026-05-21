using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notification.Core;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;

namespace Notification.Wpf.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for registering WPF notification services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class WpfNotificationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the WPF notification manager and related services in the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add the notification services to.</param>
        /// <param name="configure">An optional delegate used to configure the notification options.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance so that calls can be chained.</returns>
        public static IServiceCollection AddWpfNotifications(
            this IServiceCollection services,
            Action<INotificationConfiguration> configure = null)
        {
            services.AddNotifications(configure);

            services.TryAddSingleton<NotificationManager>(sp =>
            {
                INotificationConfiguration config = sp.GetRequiredService<INotificationConfiguration>();
                INotificationEventService events = sp.GetRequiredService<INotificationEventService>();
                NotificationConstants.Bind(config);
                NotificationArea.EventService = events;
                return new NotificationManager(config, events);
            });

            services.TryAddSingleton<INotificationManager>(sp => sp.GetRequiredService<NotificationManager>());
            services.TryAddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationManager>());

            return services;
        }
    }
}
