using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notification.Core
{
    /// <summary>
    /// Convenience overloads for <see cref="INotificationService.ShowAsync"/>.
    /// </summary>
    public static class NotificationServiceAsyncExtensions
    {
        /// <summary>Show a message-only notification asynchronously.</summary>
        public static Task<NotificationCloseResult> ShowAsync(
            this INotificationService service,
            string message,
            NotificationType type = NotificationType.None,
            TimeSpan? expirationTime = null,
            string areaName = "",
            CancellationToken cancellationToken = default)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            return service.ShowAsync(new NotificationRequest
            {
                Message = message,
                Type = type,
                ExpirationTime = expirationTime,
                AreaName = areaName ?? ""
            }, cancellationToken);
        }

        /// <summary>Show a title + message notification asynchronously.</summary>
        public static Task<NotificationCloseResult> ShowAsync(
            this INotificationService service,
            string title, string message,
            NotificationType type = NotificationType.None,
            TimeSpan? expirationTime = null,
            string areaName = "",
            CancellationToken cancellationToken = default)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            return service.ShowAsync(new NotificationRequest
            {
                Title = title,
                Message = message,
                Type = type,
                ExpirationTime = expirationTime,
                AreaName = areaName ?? ""
            }, cancellationToken);
        }
    }
}
