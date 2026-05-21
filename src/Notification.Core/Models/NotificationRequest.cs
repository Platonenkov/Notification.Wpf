using System;
using System.Collections.Generic;

namespace Notification.Core
{
    /// <summary>
    /// Represents a complete request to display a notification, including all visual and behavioral options.
    /// </summary>
    public sealed class NotificationRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier for this notification request.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the notification title text.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the notification message body text.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the notification type that determines the visual style.
        /// </summary>
        public NotificationType Type { get; set; } = NotificationType.None;

        /// <summary>
        /// Gets or sets the name of the notification area where this notification should be displayed.
        /// </summary>
        public string AreaName { get; set; } = "";

        /// <summary>
        /// Gets or sets the duration after which the notification automatically closes. When null, the default expiration time from configuration is used.
        /// </summary>
        public TimeSpan? ExpirationTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification is closed when clicked.
        /// </summary>
        public bool CloseOnClick { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the close button is visible.
        /// </summary>
        public bool ShowCloseButton { get; set; } = true;

        /// <summary>
        /// Gets or sets the display priority of this notification.
        /// </summary>
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>
        /// Gets or sets the group key used to deduplicate notifications. Notifications with the same group key replace each other.
        /// </summary>
        public string GroupKey { get; set; }

        /// <summary>
        /// Gets or sets the custom background color. When null, the default color from configuration is used.
        /// </summary>
        public NotificationColor? BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the custom foreground (text) color. When null, the default color from configuration is used.
        /// </summary>
        public NotificationColor? ForegroundColor { get; set; }

        /// <summary>
        /// Gets or sets the text rendering settings for the title.
        /// </summary>
        public NotificationTextSettings TitleSettings { get; set; }

        /// <summary>
        /// Gets or sets the text rendering settings for the message.
        /// </summary>
        public NotificationTextSettings MessageSettings { get; set; }

        /// <summary>
        /// Gets or sets the text trimming type applied to the notification message.
        /// </summary>
        public NotificationTextTrimType TrimType { get; set; } = NotificationTextTrimType.NoTrim;

        /// <summary>
        /// Gets or sets the maximum number of visible text rows before trimming is applied.
        /// </summary>
        public uint RowsCount { get; set; } = 2;

        /// <summary>
        /// Gets or sets the action invoked when the left button is clicked.
        /// </summary>
        public Action LeftButtonAction { get; set; }

        /// <summary>
        /// Gets or sets the text content displayed on the left button.
        /// </summary>
        public string LeftButtonContent { get; set; }

        /// <summary>
        /// Gets or sets the action invoked when the right button is clicked.
        /// </summary>
        public Action RightButtonAction { get; set; }

        /// <summary>
        /// Gets or sets the text content displayed on the right button.
        /// </summary>
        public string RightButtonContent { get; set; }

        /// <summary>
        /// Gets or sets the action invoked when the notification body is clicked.
        /// </summary>
        public Action OnClick { get; set; }

        /// <summary>
        /// Gets or sets the action invoked when the notification is closed.
        /// </summary>
        public Action OnClose { get; set; }

        /// <summary>
        /// Gets or sets the icon displayed in the notification. The type is platform-dependent.
        /// </summary>
        public object Icon { get; set; }

        /// <summary>
        /// Gets or sets the platform-specific image object for the notification.
        /// </summary>
        public object PlatformImage { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of custom extension data attached to this notification.
        /// </summary>
        public IDictionary<string, object> Extensions { get; set; }
    }
}
