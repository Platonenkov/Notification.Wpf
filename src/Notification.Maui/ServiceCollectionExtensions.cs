using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Notification.Core;

namespace Notification.Maui
{
    /// <summary>
    /// Provides extension methods for registering MAUI notification services.
    /// </summary>
    public static class MauiNotificationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the MAUI notification manager and core notification services.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="configure">An optional delegate to configure the notification settings.</param>
        /// <returns>The same service collection instance for chaining.</returns>
        public static IServiceCollection AddMauiNotifications(
            this IServiceCollection services,
            Action<INotificationConfiguration> configure = null)
        {
            services.AddNotifications(configure);

            services.TryAddSingleton<MauiNotificationManager>(sp =>
            {
                INotificationConfiguration config = sp.GetRequiredService<INotificationConfiguration>();
                INotificationEventService events = sp.GetRequiredService<INotificationEventService>();
                return new MauiNotificationManager(config, events);
            });

            services.TryAddSingleton<INotificationService>(sp => sp.GetRequiredService<MauiNotificationManager>());

            return services;
        }

        /// <summary>
        /// Registers the MAUI notification services on the application builder.
        /// </summary>
        /// <param name="builder">The MAUI application builder.</param>
        /// <param name="configure">An optional delegate to configure the notification settings.</param>
        /// <returns>The same application builder instance for chaining.</returns>
        public static MauiAppBuilder UseMauiNotifications(
            this MauiAppBuilder builder,
            Action<INotificationConfiguration> configure = null)
        {
            builder.Services.AddMauiNotifications(configure);
            return builder;
        }
    }
}
