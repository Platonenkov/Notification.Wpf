namespace Notification.Core
{
    /// <summary>
    /// Specifies the priority level of a notification, affecting its display order.
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>
        /// Low priority; displayed after higher-priority notifications.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal (default) priority.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority; displayed before normal and low-priority notifications.
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority; displayed immediately above all other notifications.
        /// </summary>
        Critical = 3
    }
}
