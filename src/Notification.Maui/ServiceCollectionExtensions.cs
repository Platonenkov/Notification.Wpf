using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Notification.Core;

namespace Notification.Maui
{
    public static class MauiNotificationServiceCollectionExtensions
    {
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

        public static MauiAppBuilder UseMauiNotifications(
            this MauiAppBuilder builder,
            Action<INotificationConfiguration> configure = null)
        {
            builder.Services.AddMauiNotifications(configure);
            return builder;
        }
    }
}
