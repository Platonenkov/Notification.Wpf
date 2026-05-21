using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides methods for displaying and managing notifications.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Displays a notification based on the specified request.
        /// </summary>
        /// <param name="request">The notification request containing content and configuration.</param>
        /// <returns>A unique identifier for the displayed notification.</returns>
        Guid Show(NotificationRequest request);

        /// <summary>
        /// Dismisses a specific notification by its identifier.
        /// </summary>
        /// <param name="notificationId">The unique identifier of the notification to dismiss.</param>
        void Dismiss(Guid notificationId);

        /// <summary>
        /// Dismisses all currently displayed notifications.
        /// </summary>
        void DismissAll();
    }
}
