using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notification.Core;

namespace Notification.Avalonia
{
    public static class AvaloniaNotificationServiceCollectionExtensions
    {
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
