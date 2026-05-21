using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides event data for notification lifecycle events such as shown, clicked, closed, and dismissed.
    /// </summary>
    public sealed class NotificationLifecycleEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier of the notification that triggered this event.
        /// </summary>
        public Guid NotificationId { get; }

        /// <summary>
        /// Gets the lifecycle stage that the notification has entered.
        /// </summary>
        public NotificationLifecycleStage Stage { get; }

        /// <summary>
        /// Gets the UTC timestamp when this lifecycle event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the title of the notification at the time of the event.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the message of the notification at the time of the event.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationLifecycleEventArgs"/> class.
        /// </summary>
        /// <param name="notificationId">The unique identifier of the notification.</param>
        /// <param name="stage">The lifecycle stage that the notification has entered.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        public NotificationLifecycleEventArgs(
            Guid notificationId,
            NotificationLifecycleStage stage,
            string title,
            string message)
        {
            NotificationId = notificationId;
            Stage = stage;
            Timestamp = DateTimeOffset.UtcNow;
            Title = title;
            Message = message;
        }
    }
}
