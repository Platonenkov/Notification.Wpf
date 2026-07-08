using System;

namespace Notification.Core
{
    /// <summary>
    /// Result of an asynchronous notification or dialog close.
    /// </summary>
    public sealed class NotificationCloseResult
    {
        /// <summary>The unique identifier of the closed notification.</summary>
        public Guid NotificationId { get; init; }

        /// <summary>Why the notification was closed.</summary>
        public NotificationCloseReason Reason { get; init; }

        /// <summary>The identifier of the action button that closed the notification, or <c>null</c>.</summary>
        public string ButtonId { get; init; }

        /// <summary>The zero-based index of the action button that closed the notification, or <c>null</c>.</summary>
        public int? ButtonIndex { get; init; }

        /// <summary>True when <see cref="Reason"/> is <see cref="NotificationCloseReason.Cancelled"/>.</summary>
        public bool IsCancelled => Reason == NotificationCloseReason.Cancelled;
    }
}
