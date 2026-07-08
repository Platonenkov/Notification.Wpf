namespace Notification.Core
{
    /// <summary>
    /// An action button shown on a notification or dialog.
    /// </summary>
    public sealed class NotificationButton
    {
        /// <summary>Stable identifier. Returned in <see cref="NotificationCloseResult.ButtonId"/>. Required and unique within a request.</summary>
        public string Id { get; init; }

        /// <summary>User-visible text on the button.</summary>
        public string Content { get; init; }

        /// <summary>Role hint for platform styling.</summary>
        public NotificationButtonRole Role { get; init; } = NotificationButtonRole.Default;
    }
}
