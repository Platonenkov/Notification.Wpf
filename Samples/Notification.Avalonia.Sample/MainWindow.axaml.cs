using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Notification.Core;

namespace Notification.Avalonia.Sample
{
    public partial class MainWindow : Window
    {
        private readonly INotificationService _service;
        private readonly INotificationEventService _events;
        private readonly AvaloniaNotificationManager _manager;

        public MainWindow()
        {
            InitializeComponent();

            ServiceCollection services = new ServiceCollection();
            services.AddAvaloniaNotifications(config =>
            {
                config.DefaultExpirationTime = TimeSpan.FromSeconds(5);
            });

            ServiceProvider provider = services.BuildServiceProvider();

            _manager = provider.GetRequiredService<AvaloniaNotificationManager>();
            _manager.SetHost(this);

            _service = provider.GetRequiredService<INotificationService>();
            _events = provider.GetRequiredService<INotificationEventService>();

            _events.NotificationLifecycleChanged += (sender, e) =>
            {
                TextBlock log = this.FindControl<TextBlock>("TxtLog");
                if (log != null)
                    log.Text = $"Last event: {e.Stage} - {e.Title ?? "N/A"} ({e.NotificationId:N})";
            };
        }

        private void OnShowSuccess(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder.Create("Success", "Operation completed").AsSuccess().Build());
        }

        private void OnShowWarning(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder.Create("Warning", "Disk space low").AsWarning().Build());
        }

        private void OnShowError(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder.Create("Error", "Connection failed").AsError().NeverExpires().Build());
        }

        private void OnShowInfo(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder.Create("Info", "Update available").AsInformation().Build());
        }

        private void OnBuilderDemo(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder
                .Create()
                .WithTitle("Builder Demo")
                .WithMessage("Created with fluent builder API")
                .OfType(NotificationType.Success)
                .ExpiresInSeconds(8)
                .WithPriority(NotificationPriority.High)
                .OnClick(() =>
                {
                    TextBlock log = this.FindControl<TextBlock>("TxtLog");
                    if (log != null)
                        log.Text = "Notification was clicked!";
                })
                .Build());
        }

        private void OnDismissAll(object sender, RoutedEventArgs e)
        {
            _service.DismissAll();
        }
    }
}
