using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides event data for notification lifecycle events such as shown, clicked, closed, and dismissed.
    /// </summary>
    public sealed class NotificationLifecycleEventArgs : EventArgs
    {
        /// <summary>Gets the unique identifier of the notification that triggered this event.</summary>
        public Guid NotificationId { get; }

        /// <summary>Gets the lifecycle stage that the notification has entered.</summary>
        public NotificationLifecycleStage Stage { get; }

        /// <summary>Gets the UTC timestamp when this lifecycle event occurred.</summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>Gets the title of the notification at the time of the event.</summary>
        public string Title { get; }

        /// <summary>Gets the message of the notification at the time of the event.</summary>
        public string Message { get; }

        /// <summary>
        /// Gets why the notification was closed. Populated only for
        /// <see cref="NotificationLifecycleStage.Closed"/> and <see cref="NotificationLifecycleStage.Dismissed"/>.
        /// </summary>
        public NotificationCloseReason? CloseReason { get; }

        /// <summary>Initializes a lifecycle event without a close reason.</summary>
        public NotificationLifecycleEventArgs(
            Guid notificationId,
            NotificationLifecycleStage stage,
            string title,
            string message)
            : this(notificationId, stage, title, message, closeReason: null)
        {
        }

        /// <summary>Initializes a lifecycle event with a close reason.</summary>
        public NotificationLifecycleEventArgs(
            Guid notificationId,
            NotificationLifecycleStage stage,
            string title,
            string message,
            NotificationCloseReason? closeReason)
        {
            NotificationId = notificationId;
            Stage = stage;
            Timestamp = DateTimeOffset.UtcNow;
            Title = title;
            Message = message;
            CloseReason = closeReason;
        }
    }
}
