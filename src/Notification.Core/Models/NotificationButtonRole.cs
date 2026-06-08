namespace Notification.Core
{
    /// <summary>
    /// Hint used by platform UI to style action buttons. Does not affect the
    /// <see cref="NotificationCloseReason"/> returned on click.
    /// </summary>
    public enum NotificationButtonRole
    {
        /// <summary>No special role.</summary>
        Default = 0,
        /// <summary>Confirmation (e.g. OK, Yes, Save).</summary>
        Affirmative = 1,
        /// <summary>Refusal or cancel (e.g. Cancel, No).</summary>
        Negative = 2,
        /// <summary>Destructive action (e.g. Delete). Platforms may render this as red.</summary>
        Destructive = 3
    }
}
