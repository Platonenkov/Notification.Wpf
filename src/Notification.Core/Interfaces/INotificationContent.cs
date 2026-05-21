using System;

namespace Notification.Core
{
    /// <summary>
    /// Represents the content and visual configuration of a notification.
    /// </summary>
    public interface INotificationContent
    {
        /// <summary>
        /// Gets the title text displayed at the top of the notification.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the message body text of the notification.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the type of the notification (e.g., success, warning, error, information).
        /// </summary>
        NotificationType Type { get; }

        /// <summary>
        /// Gets the custom background color override for the notification, or <c>null</c> to use the default.
        /// </summary>
        NotificationColor? BackgroundColor { get; }

        /// <summary>
        /// Gets the custom foreground color override for the notification, or <c>null</c> to use the default.
        /// </summary>
        NotificationColor? ForegroundColor { get; }

        /// <summary>
        /// Gets the text trimming type applied when content exceeds available space.
        /// </summary>
        NotificationTextTrimType TrimType { get; }

        /// <summary>
        /// Gets the maximum number of visible text rows in the notification.
        /// </summary>
        uint RowsCount { get; }

        /// <summary>
        /// Gets the text display settings for the notification title.
        /// </summary>
        NotificationTextSettings TitleSettings { get; }

        /// <summary>
        /// Gets the text display settings for the notification message.
        /// </summary>
        NotificationTextSettings MessageSettings { get; }

        /// <summary>
        /// Gets the icon displayed in the notification.
        /// </summary>
        object Icon { get; }

        /// <summary>
        /// Gets the image data displayed in the notification.
        /// </summary>
        NotificationImageData Image { get; }

        /// <summary>
        /// Gets the action invoked when the left button is clicked.
        /// </summary>
        Action LeftButtonAction { get; }

        /// <summary>
        /// Gets the content displayed on the left button.
        /// </summary>
        object LeftButtonContent { get; }

        /// <summary>
        /// Gets the action invoked when the right button is clicked.
        /// </summary>
        Action RightButtonAction { get; }

        /// <summary>
        /// Gets the content displayed on the right button.
        /// </summary>
        object RightButtonContent { get; }

        /// <summary>
        /// Gets a value indicating whether the notification is dismissed when clicked.
        /// </summary>
        bool CloseOnClick { get; }
    }
}
