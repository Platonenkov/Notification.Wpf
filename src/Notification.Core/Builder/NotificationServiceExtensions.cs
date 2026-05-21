using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides extension methods for <see cref="INotificationService"/> to simplify notification creation.
    /// </summary>
    public static class NotificationServiceExtensions
    {
        /// <summary>
        /// Shows a notification configured via a fluent builder action.
        /// </summary>
        /// <param name="service">The notification service instance.</param>
        /// <param name="configure">An action that configures the notification using a <see cref="NotificationBuilder"/>.</param>
        /// <returns>The unique identifier of the displayed notification.</returns>
        public static Guid Show(this INotificationService service, Action<NotificationBuilder> configure)
        {
            NotificationBuilder builder = NotificationBuilder.Create();
            configure(builder);
            return service.Show(builder.Build());
        }
    }
}
