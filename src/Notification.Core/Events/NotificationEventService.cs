using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides a centralized service for raising and subscribing to notification lifecycle events.
    /// </summary>
    public class NotificationEventService : INotificationEventService
    {
        /// <summary>
        /// Occurs when any notification lifecycle stage changes.
        /// </summary>
        public event EventHandler<NotificationLifecycleEventArgs> NotificationLifecycleChanged;

        /// <summary>
        /// Occurs when a notification is displayed to the user.
        /// </summary>
        public event EventHandler<NotificationLifecycleEventArgs> NotificationShown;

        /// <summary>
        /// Occurs when a notification is closed for any reason (explicit close, timeout, or dismissal).
        /// </summary>
        public event EventHandler<NotificationLifecycleEventArgs> NotificationClosed;

        /// <summary>
        /// Occurs when a notification is clicked by the user.
        /// </summary>
        public event EventHandler<NotificationLifecycleEventArgs> NotificationClicked;

        /// <summary>
        /// Raises the appropriate lifecycle events based on the notification stage.
        /// </summary>
        /// <param name="args">The event arguments containing notification details and the lifecycle stage.</param>
        public void Raise(NotificationLifecycleEventArgs args)
        {
            NotificationLifecycleChanged?.Invoke(this, args);

            switch (args.Stage)
            {
                case NotificationLifecycleStage.Shown:
                    NotificationShown?.Invoke(this, args);
                    break;
                case NotificationLifecycleStage.Clicked:
                    NotificationClicked?.Invoke(this, args);
                    break;
                case NotificationLifecycleStage.Closed:
                case NotificationLifecycleStage.TimedOut:
                case NotificationLifecycleStage.Dismissed:
                    NotificationClosed?.Invoke(this, args);
                    break;
            }
        }
    }
}
