using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notification.Console;
using Notification.Core;

namespace Notification.Console.Sample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("=== Notification.Core Console Demo ===\n");

            // --- Direct usage (without DI) ---
            System.Console.WriteLine("--- Direct usage ---\n");

            ConsoleNotificationManager manager = new ConsoleNotificationManager();

            manager.Show(NotificationBuilder
                .Create("Hello", "Basic notification via Builder API")
                .OfType(NotificationType.None)
                .Build());

            manager.Show(NotificationBuilder
                .Create("Success", "Operation completed successfully")
                .AsSuccess()
                .Build());

            manager.Show(NotificationBuilder
                .Create("Warning", "Disk space is running low")
                .AsWarning()
                .Build());

            manager.Show(NotificationBuilder
                .Create("Error", "Connection to server failed")
                .AsError()
                .Build());

            manager.Show(NotificationBuilder
                .Create("Info", "New version available: v2.0")
                .AsInformation()
                .Build());

            System.Console.WriteLine();

            // --- Progress bar demo ---
            System.Console.WriteLine("--- Progress bar ---\n");

            for (int i = 0; i <= 100; i += 5)
            {
                manager.ShowProgress("Processing files...", i);
                await Task.Delay(50);
            }

            System.Console.WriteLine();

            // --- DI usage ---
            System.Console.WriteLine("--- DI usage ---\n");

            ServiceCollection services = new ServiceCollection();
            services.AddConsoleNotifications(config =>
            {
                config.DefaultExpirationTime = TimeSpan.FromSeconds(3);
            });

            ServiceProvider provider = services.BuildServiceProvider();

            INotificationService notificationService = provider.GetRequiredService<INotificationService>();
            INotificationEventService eventService = provider.GetRequiredService<INotificationEventService>();

            eventService.NotificationShown += (sender, e) =>
                System.Console.WriteLine($"  [Event] Shown: {e.NotificationId:N} - {e.Title}");

            Guid id = notificationService.Show(NotificationBuilder
                .Create("DI Notification", "This was shown via INotificationService from DI container")
                .AsInformation()
                .WithPriority(NotificationPriority.High)
                .Build());

            System.Console.WriteLine($"\n  Notification ID: {id}");

            notificationService.Dismiss(id);
            System.Console.WriteLine($"  Dismissed: {id}\n");

            // --- Builder with buttons ---
            System.Console.WriteLine("--- Builder with callbacks ---\n");

            notificationService.Show(NotificationBuilder
                .Create("Action Required", "Click to acknowledge")
                .AsWarning()
                .OnClick(() => System.Console.WriteLine("  [Callback] Notification clicked!"))
                .OnClose(() => System.Console.WriteLine("  [Callback] Notification closed!"))
                .Build());

            System.Console.WriteLine("\n=== Demo complete ===");
        }
    }
}
