using System;

namespace Notification.Core
{
    /// <summary>
    /// Represents the content data for a notification, including text, colors, buttons, and media.
    /// </summary>
    public class NotificationContentData : INotificationContent
    {
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
        /// Gets or sets the custom background color. When null, the default color from configuration is used.
        /// </summary>
        public NotificationColor? BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the custom foreground (text) color. When null, the default color from configuration is used.
        /// </summary>
        public NotificationColor? ForegroundColor { get; set; }

        /// <summary>
        /// Gets or sets the text trimming type applied to the notification message.
        /// </summary>
        public NotificationTextTrimType TrimType { get; set; } = NotificationTextTrimType.NoTrim;

        /// <summary>
        /// Gets or sets the maximum number of visible text rows before trimming is applied.
        /// </summary>
        public uint RowsCount { get; set; } = 2;

        /// <summary>
        /// Gets or sets the text rendering settings for the title.
        /// </summary>
        public NotificationTextSettings TitleSettings { get; set; }

        /// <summary>
        /// Gets or sets the text rendering settings for the message.
        /// </summary>
        public NotificationTextSettings MessageSettings { get; set; }

        /// <summary>
        /// Gets or sets the icon displayed in the notification. The type is platform-dependent.
        /// </summary>
        public object Icon { get; set; }

        /// <summary>
        /// Gets or sets the image data displayed in the notification.
        /// </summary>
        public NotificationImageData Image { get; set; }

        /// <summary>
        /// Gets or sets the action invoked when the left button is clicked.
        /// </summary>
        public Action LeftButtonAction { get; set; }

        /// <summary>
        /// Gets or sets the content displayed on the left button.
        /// </summary>
        public object LeftButtonContent { get; set; }

        /// <summary>
        /// Gets or sets the action invoked when the right button is clicked.
        /// </summary>
        public Action RightButtonAction { get; set; }

        /// <summary>
        /// Gets or sets the content displayed on the right button.
        /// </summary>
        public object RightButtonContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification is closed when clicked.
        /// </summary>
        public bool CloseOnClick { get; set; } = true;
    }
}
