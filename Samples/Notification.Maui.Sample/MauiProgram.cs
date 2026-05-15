using System;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Notification.Core;

namespace Notification.Maui.Sample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
#if WINDOWS
                    options.SetShouldEnableSnackbarOnWindows(true);
#endif
                })
                .UseMauiNotifications(config =>
                {
                    config.DefaultExpirationTime = TimeSpan.FromSeconds(3);
                });

            return builder.Build();
        }
    }
}
