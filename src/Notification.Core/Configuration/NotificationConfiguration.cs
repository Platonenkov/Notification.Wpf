using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides default configuration settings for the notification system, including positioning, sizing, colors, and text formatting.
    /// </summary>
    public class NotificationConfiguration : INotificationConfiguration
    {
        /// <summary>
        /// Gets or sets the screen position where notifications are displayed.
        /// </summary>
        public NotificationPosition MessagePosition { get; set; } = NotificationPosition.BottomRight;

        /// <summary>
        /// Gets or sets a value indicating whether the notification panel order is reversed. When null, the default behavior is used.
        /// </summary>
        public bool? IsReversedPanel { get; set; }

        /// <summary>
        /// Gets or sets the minimum width of a notification in pixels.
        /// </summary>
        public double MinWidth { get; set; } = 350.0;

        /// <summary>
        /// Gets or sets the maximum width of a notification in pixels.
        /// </summary>
        public double MaxWidth { get; set; } = 350.0;

        /// <summary>
        /// Gets or sets the maximum number of notification overlay windows that can be displayed simultaneously.
        /// </summary>
        public uint MaxOverlayWindowCount { get; set; } = 999;

        /// <summary>
        /// Gets or sets a value indicating whether progress bars are automatically collapsed when there are more rows than the limit.
        /// </summary>
        public bool CollapseProgressIfMoreRows { get; set; } = true;

        /// <summary>
        /// Gets or sets the default expiration time for notifications that do not specify one explicitly.
        /// </summary>
        public TimeSpan DefaultExpirationTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the default background color for notifications without a specific type.
        /// </summary>
        public NotificationColor DefaultBackgroundColor { get; set; } = NotificationColor.FromHex("#FF444444");

        /// <summary>
        /// Gets or sets the default foreground (text) color for notifications.
        /// </summary>
        public NotificationColor DefaultForegroundColor { get; set; } = NotificationColor.WhiteSmoke;

        /// <summary>
        /// Gets or sets the background color for success notifications.
        /// </summary>
        public NotificationColor SuccessBackgroundColor { get; set; } = NotificationColor.LimeGreen;

        /// <summary>
        /// Gets or sets the background color for warning notifications.
        /// </summary>
        public NotificationColor WarningBackgroundColor { get; set; } = NotificationColor.Orange;

        /// <summary>
        /// Gets or sets the background color for error notifications.
        /// </summary>
        public NotificationColor ErrorBackgroundColor { get; set; } = NotificationColor.OrangeRed;

        /// <summary>
        /// Gets or sets the background color for information notifications.
        /// </summary>
        public NotificationColor InformationBackgroundColor { get; set; } = NotificationColor.CornflowerBlue;

        /// <summary>
        /// Gets or sets the default color for progress bar indicators.
        /// </summary>
        public NotificationColor DefaultProgressColor { get; set; } = NotificationColor.Green;

        /// <summary>
        /// Gets or sets the base text size in pixels.
        /// </summary>
        public double BaseTextSize { get; set; } = 14.0;

        /// <summary>
        /// Gets or sets the title text size in pixels.
        /// </summary>
        public double TitleSize { get; set; } = 14.0;

        /// <summary>
        /// Gets or sets the message text size in pixels.
        /// </summary>
        public double MessageSize { get; set; } = 14.0;

        /// <summary>
        /// Gets or sets the font family name used for notification text.
        /// </summary>
        public string FontName { get; set; } = "Segoe UI";

        /// <summary>
        /// Gets or sets the text alignment for notification titles.
        /// </summary>
        public NotificationTextAlignment TitleTextAlignment { get; set; } = NotificationTextAlignment.Left;

        /// <summary>
        /// Gets or sets the text alignment for notification messages.
        /// </summary>
        public NotificationTextAlignment MessageTextAlignment { get; set; } = NotificationTextAlignment.Left;

        /// <summary>
        /// Gets or sets the default number of visible text rows before trimming.
        /// </summary>
        public uint DefaultRowCount { get; set; } = 2;

        /// <summary>
        /// Gets or sets the default text trimming type applied to notification text.
        /// </summary>
        public NotificationTextTrimType DefaultTrimType { get; set; } = NotificationTextTrimType.NoTrim;

        /// <summary>
        /// Gets or sets the message displayed when an operation is cancelled.
        /// </summary>
        public string CancellationMessage { get; set; } = "Operation was cancelled";

        /// <summary>
        /// Gets or sets the label text for the "Open File" action.
        /// </summary>
        public string OpenFileMessage { get; set; } = "Open File";

        /// <summary>
        /// Gets or sets the label text for the "Open Folder" action.
        /// </summary>
        public string OpenFolderMessage { get; set; } = "Open Folder";

        /// <summary>
        /// Gets or sets the default content for the left button.
        /// </summary>
        public string DefaultLeftButtonContent { get; set; } = "Ok";

        /// <summary>
        /// Gets or sets the default content for the right button.
        /// </summary>
        public string DefaultRightButtonContent { get; set; } = "Cancel";

        /// <summary>
        /// Gets or sets the default content for the progress bar cancel button.
        /// </summary>
        public string DefaultProgressButtonContent { get; set; } = "Cancel";
    }
}
