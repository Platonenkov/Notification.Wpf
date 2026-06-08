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
            string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "default";

            switch (mode)
            {
                case "async":
                    await RunAsyncDemo();
                    break;
                case "dialog":
                    await RunDialogDemo();
                    break;
                default:
                    await RunDefaultDemo();
                    break;
            }
        }

        // ----------------------------------------------------------------
        // Async API demo
        // ----------------------------------------------------------------
        private static async Task RunAsyncDemo()
        {
            System.Console.WriteLine("=== Async API Demo ===\n");

            ServiceCollection services = new ServiceCollection();
            services.AddConsoleNotifications(config =>
            {
                config.DefaultExpirationTime = TimeSpan.FromSeconds(2);
            });

            ServiceProvider provider = services.BuildServiceProvider();
            INotificationService mgr = provider.GetRequiredService<INotificationService>();

            // --- ShowAsync simple ---
            System.Console.WriteLine("--- ShowAsync (simple) ---");
            NotificationCloseResult r1 = await mgr.ShowAsync("Starting", NotificationType.Information);
            System.Console.WriteLine($"First:  Reason={r1.Reason}  ButtonId={r1.ButtonId ?? "—"}\n");

            // --- ShowDialogAsync 2 buttons ---
            System.Console.WriteLine("--- ShowDialogAsync (Delete / Cancel) ---");
            NotificationCloseResult r2 = await mgr.ShowDialogAsync(
                new NotificationRequest
                {
                    Title   = "Confirm Delete",
                    Message = "Are you sure?",
                    Type    = NotificationType.Warning,
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "delete", Content = "Delete", Role = NotificationButtonRole.Affirmative },
                        new NotificationButton { Id = "cancel", Content = "Cancel", Role = NotificationButtonRole.Negative }
                    }
                },
                NotificationDialogFallback.ShowFirstAffirmative);

            System.Console.WriteLine($"Picked: {r2.ButtonId}  (Reason={r2.Reason})\n");

            // --- ShowDialogAsync fallback = ShowFirstAffirmative ---
            System.Console.WriteLine("--- ShowDialogAsync (Yes/No/Maybe, fallback=ShowFirstAffirmative) ---");
            NotificationCloseResult r3 = await mgr.ShowDialogAsync(
                new NotificationRequest
                {
                    Message = "Proceed?",
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "ok",    Content = "OK",    Role = NotificationButtonRole.Affirmative },
                        new NotificationButton { Id = "no",    Content = "No",    Role = NotificationButtonRole.Negative },
                        new NotificationButton { Id = "maybe", Content = "Maybe", Role = NotificationButtonRole.Default }
                    }
                },
                NotificationDialogFallback.ShowFirstAffirmative);

            System.Console.WriteLine($"Picked: {r3.ButtonId}  (Reason={r3.Reason})\n");

            // --- ShowDialogAsync fallback = SkipSilently ---
            System.Console.WriteLine("--- ShowDialogAsync (3 buttons, fallback=SkipSilently) ---");
            NotificationCloseResult r4 = await mgr.ShowDialogAsync(
                new NotificationRequest
                {
                    Message = "Skipped silently?",
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "yes",   Content = "Yes",   Role = NotificationButtonRole.Affirmative },
                        new NotificationButton { Id = "no",    Content = "No",    Role = NotificationButtonRole.Negative },
                        new NotificationButton { Id = "maybe", Content = "Maybe", Role = NotificationButtonRole.Default }
                    }
                },
                NotificationDialogFallback.SkipSilently);

            System.Console.WriteLine($"SkipSilently result: Reason={r4.Reason}  ButtonId={r4.ButtonId ?? "—"}\n");

            // --- ShowDialogAsync fallback = Throw ---
            System.Console.WriteLine("--- ShowDialogAsync (3 buttons, fallback=Throw) ---");
            try
            {
                await mgr.ShowDialogAsync(
                    new NotificationRequest
                    {
                        Message = "This should throw",
                        Buttons = new[]
                        {
                            new NotificationButton { Id = "yes",   Content = "Yes",   Role = NotificationButtonRole.Affirmative },
                            new NotificationButton { Id = "no",    Content = "No",    Role = NotificationButtonRole.Negative },
                            new NotificationButton { Id = "maybe", Content = "Maybe", Role = NotificationButtonRole.Default }
                        }
                    },
                    NotificationDialogFallback.Throw);

                System.Console.WriteLine("  (no exception — platform handled it)");
            }
            catch (NotSupportedException ex)
            {
                System.Console.WriteLine($"  NotSupportedException caught (expected): {ex.Message}\n");
            }

            System.Console.WriteLine("=== Async API Demo complete ===");
        }

        // ----------------------------------------------------------------
        // Dialog-only demo (args[0] = "dialog")
        // ----------------------------------------------------------------
        private static async Task RunDialogDemo()
        {
            System.Console.WriteLine("=== Dialog Demo ===\n");

            ServiceCollection services = new ServiceCollection();
            services.AddConsoleNotifications();
            ServiceProvider provider = services.BuildServiceProvider();
            INotificationService mgr = provider.GetRequiredService<INotificationService>();

            NotificationCloseResult result = await mgr.ShowDialogAsync(
                new NotificationRequest
                {
                    Message = "Proceed?",
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "ok", Content = "OK", Role = NotificationButtonRole.Affirmative },
                        new NotificationButton { Id = "no", Content = "No", Role = NotificationButtonRole.Negative }
                    }
                },
                NotificationDialogFallback.ShowFirstAffirmative);

            System.Console.WriteLine($"Picked: {result.ButtonId}  (Reason={result.Reason})");
            System.Console.WriteLine("\n=== Dialog Demo complete ===");
        }

        // ----------------------------------------------------------------
        // Default demo (original)
        // ----------------------------------------------------------------
        private static async Task RunDefaultDemo()
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
