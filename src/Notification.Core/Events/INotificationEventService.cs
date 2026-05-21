using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides events for tracking notification lifecycle changes.
    /// </summary>
    public interface INotificationEventService
    {
        /// <summary>
        /// Occurs when any notification lifecycle stage changes.
        /// </summary>
        event EventHandler<NotificationLifecycleEventArgs> NotificationLifecycleChanged;

        /// <summary>
        /// Occurs when a notification is displayed to the user.
        /// </summary>
        event EventHandler<NotificationLifecycleEventArgs> NotificationShown;

        /// <summary>
        /// Occurs when a notification is closed or dismissed.
        /// </summary>
        event EventHandler<NotificationLifecycleEventArgs> NotificationClosed;

        /// <summary>
        /// Occurs when a notification is clicked by the user.
        /// </summary>
        event EventHandler<NotificationLifecycleEventArgs> NotificationClicked;

        /// <summary>
        /// Raises a notification lifecycle event.
        /// </summary>
        /// <param name="args">The event arguments containing the notification identifier and lifecycle stage.</param>
        void Raise(NotificationLifecycleEventArgs args);
    }
}
