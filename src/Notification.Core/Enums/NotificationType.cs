namespace Notification.Core
{
    /// <summary>
    /// Specifies the type of a notification, which determines its visual appearance and semantics.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// No specific type; uses default styling.
        /// </summary>
        None,

        /// <summary>
        /// An informational notification.
        /// </summary>
        Information,

        /// <summary>
        /// A success notification indicating a completed operation.
        /// </summary>
        Success,

        /// <summary>
        /// A warning notification indicating a potential issue.
        /// </summary>
        Warning,

        /// <summary>
        /// An error notification indicating a failure or critical issue.
        /// </summary>
        Error,

        /// <summary>
        /// A general-purpose notification.
        /// </summary>
        Notification
    }
}
