namespace Notification.Core
{
    /// <summary>
    /// Describes why a notification was closed.
    /// </summary>
    public enum NotificationCloseReason
    {
        /// <summary>The expiration time elapsed.</summary>
        Timeout = 0,
        /// <summary>The user clicked the notification body while <see cref="NotificationRequest.CloseOnClick"/> was true.</summary>
        BodyClick = 1,
        /// <summary>The user pressed the close (X) button.</summary>
        XButton = 2,
        /// <summary>The user pressed one of the action buttons.</summary>
        ActionButton = 3,
        /// <summary>The notification was cancelled programmatically via <see cref="System.Threading.CancellationToken"/> or <see cref="INotificationService.Dismiss"/>.</summary>
        Cancelled = 4
    }
}
