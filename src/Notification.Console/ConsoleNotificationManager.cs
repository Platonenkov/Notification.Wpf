using System;
using System.Collections.Concurrent;
using Notification.Core;

namespace Notification.Console
{
    /// <summary>
    /// Renders notifications to the system console with colored output and progress bars.
    /// </summary>
    public class ConsoleNotificationManager : INotificationService
    {
        private readonly INotificationConfiguration _config;
        private readonly INotificationEventService _events;
        private readonly ConcurrentDictionary<Guid, bool> _activeNotifications = new ConcurrentDictionary<Guid, bool>();
        private readonly object _consoleLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleNotificationManager"/> class.
        /// </summary>
        public ConsoleNotificationManager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleNotificationManager"/> class
        /// with the specified configuration and event service.
        /// </summary>
        /// <param name="config">The notification configuration.</param>
        /// <param name="events">The event service used to raise lifecycle events.</param>
        public ConsoleNotificationManager(INotificationConfiguration config, INotificationEventService events)
        {
            _config = config;
            _events = events;
        }

        /// <summary>
        /// Displays the specified notification in the console.
        /// </summary>
        /// <param name="request">The notification request to display.</param>
        /// <returns>The unique identifier of the displayed notification.</returns>
        public Guid Show(NotificationRequest request)
        {
            Guid id = request.Id;
            _activeNotifications.TryAdd(id, true);

            lock (_consoleLock)
            {
                ConsoleColor originalFg = System.Console.ForegroundColor;
                ConsoleColor originalBg = System.Console.BackgroundColor;

                try
                {
                    ConsoleColor typeColor = GetColorForType(request.Type);
                    string typePrefix = GetPrefixForType(request.Type);

                    System.Console.ForegroundColor = typeColor;
                    System.Console.Write(typePrefix);
                    System.Console.ForegroundColor = originalFg;

                    if (!string.IsNullOrEmpty(request.Title))
                    {
                        System.Console.ForegroundColor = ConsoleColor.White;
                        System.Console.Write(request.Title);
                        System.Console.ForegroundColor = originalFg;

                        if (!string.IsNullOrEmpty(request.Message))
                            System.Console.Write(" - ");
                    }

                    if (!string.IsNullOrEmpty(request.Message))
                        System.Console.Write(request.Message);

                    System.Console.WriteLine();
                }
                finally
                {
                    System.Console.ForegroundColor = originalFg;
                    System.Console.BackgroundColor = originalBg;
                }
            }

            _events?.Raise(new NotificationLifecycleEventArgs(
                id, NotificationLifecycleStage.Shown, request.Title, request.Message));

            return id;
        }

        /// <summary>
        /// Dismisses the notification with the specified identifier.
        /// </summary>
        /// <param name="notificationId">The identifier of the notification to dismiss.</param>
        public void Dismiss(Guid notificationId)
        {
            bool removed;
            _activeNotifications.TryRemove(notificationId, out removed);
            _events?.Raise(new NotificationLifecycleEventArgs(
                notificationId, NotificationLifecycleStage.Dismissed, null, null));
        }

        /// <summary>
        /// Dismisses all currently active notifications.
        /// </summary>
        public void DismissAll()
        {
            foreach (System.Collections.Generic.KeyValuePair<Guid, bool> kvp in _activeNotifications.ToArray())
            {
                Dismiss(kvp.Key);
            }
        }

        /// <summary>
        /// Renders an inline progress bar in the console.
        /// </summary>
        /// <param name="title">The text displayed next to the progress bar.</param>
        /// <param name="percent">The completion percentage in the range 0 to 100.</param>
        public void ShowProgress(string title, double percent)
        {
            lock (_consoleLock)
            {
                int barWidth = 30;
                int filled = (int)(percent / 100.0 * barWidth);

                System.Console.Write("\r");
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.Write("[");

                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write(new string('█', filled));
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.Write(new string('░', barWidth - filled));

                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.Write("]");

                System.Console.ResetColor();
                System.Console.Write($" {percent:F0}% {title}    ");

                if (percent >= 100)
                    System.Console.WriteLine();
            }
        }

        private static ConsoleColor GetColorForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return ConsoleColor.Green;
                case NotificationType.Warning: return ConsoleColor.Yellow;
                case NotificationType.Error: return ConsoleColor.Red;
                case NotificationType.Information: return ConsoleColor.Cyan;
                default: return ConsoleColor.Gray;
            }
        }

        private static string GetPrefixForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return "[OK] ";
                case NotificationType.Warning: return "[WARN] ";
                case NotificationType.Error: return "[ERR] ";
                case NotificationType.Information: return "[INFO] ";
                default: return "[NOTE] ";
            }
        }
    }
}
