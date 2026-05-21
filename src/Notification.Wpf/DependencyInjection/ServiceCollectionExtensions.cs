using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notification.Core;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;

namespace Notification.Wpf.DependencyInjection
{
    public static class WpfNotificationServiceCollectionExtensions
    {
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
