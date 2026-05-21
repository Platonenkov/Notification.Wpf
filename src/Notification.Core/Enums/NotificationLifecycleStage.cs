namespace Notification.Core
{
    /// <summary>
    /// Represents the lifecycle stage of a notification.
    /// </summary>
    public enum NotificationLifecycleStage
    {
        /// <summary>
        /// The notification has been displayed to the user.
        /// </summary>
        Shown,

        /// <summary>
        /// The notification was clicked by the user.
        /// </summary>
        Clicked,

        /// <summary>
        /// The notification was explicitly closed.
        /// </summary>
        Closed,

        /// <summary>
        /// The notification expired after its display duration elapsed.
        /// </summary>
        TimedOut,

        /// <summary>
        /// The notification was dismissed by the user.
        /// </summary>
        Dismissed
    }
}
