using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides configuration options for the notification system appearance and behavior.
    /// </summary>
    public interface INotificationConfiguration
    {
        /// <summary>
        /// Gets or sets the position on screen where notifications are displayed.
        /// </summary>
        NotificationPosition MessagePosition { get; set; }

        /// <summary>
        /// Gets or sets whether the notification panel stacking order is reversed.
        /// </summary>
        bool? IsReversedPanel { get; set; }

        /// <summary>
        /// Gets or sets the minimum width of the notification window in pixels.
        /// </summary>
        double MinWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum width of the notification window in pixels.
        /// </summary>
        double MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of overlay notification windows displayed simultaneously.
        /// </summary>
        uint MaxOverlayWindowCount { get; set; }

        /// <summary>
        /// Gets or sets whether to collapse progress notifications when more rows are displayed than allowed.
        /// </summary>
        bool CollapseProgressIfMoreRows { get; set; }

        /// <summary>
        /// Gets or sets the default time after which a notification automatically expires.
        /// </summary>
        TimeSpan DefaultExpirationTime { get; set; }

        /// <summary>
        /// Gets or sets the default background color for notifications.
        /// </summary>
        NotificationColor DefaultBackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the default foreground (text) color for notifications.
        /// </summary>
        NotificationColor DefaultForegroundColor { get; set; }

        /// <summary>
        /// Gets or sets the background color for success notifications.
        /// </summary>
        NotificationColor SuccessBackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the background color for warning notifications.
        /// </summary>
        NotificationColor WarningBackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the background color for error notifications.
        /// </summary>
        NotificationColor ErrorBackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the background color for informational notifications.
        /// </summary>
        NotificationColor InformationBackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the default color for progress indicators.
        /// </summary>
        NotificationColor DefaultProgressColor { get; set; }

        /// <summary>
        /// Gets or sets the base text size in pixels used as a reference for scaling.
        /// </summary>
        double BaseTextSize { get; set; }

        /// <summary>
        /// Gets or sets the font size for notification titles in pixels.
        /// </summary>
        double TitleSize { get; set; }

        /// <summary>
        /// Gets or sets the font size for notification messages in pixels.
        /// </summary>
        double MessageSize { get; set; }

        /// <summary>
        /// Gets or sets the font family name used for notification text.
        /// </summary>
        string FontName { get; set; }

        /// <summary>
        /// Gets or sets the text alignment for notification titles.
        /// </summary>
        NotificationTextAlignment TitleTextAlignment { get; set; }

        /// <summary>
        /// Gets or sets the text alignment for notification messages.
        /// </summary>
        NotificationTextAlignment MessageTextAlignment { get; set; }

        /// <summary>
        /// Gets or sets the default number of visible text rows in a notification.
        /// </summary>
        uint DefaultRowCount { get; set; }

        /// <summary>
        /// Gets or sets the default text trimming type when content exceeds available space.
        /// </summary>
        NotificationTextTrimType DefaultTrimType { get; set; }

        /// <summary>
        /// Gets or sets the display text for the cancellation action.
        /// </summary>
        string CancellationMessage { get; set; }

        /// <summary>
        /// Gets or sets the display text for the open file action.
        /// </summary>
        string OpenFileMessage { get; set; }

        /// <summary>
        /// Gets or sets the display text for the open folder action.
        /// </summary>
        string OpenFolderMessage { get; set; }

        /// <summary>
        /// Gets or sets the default content displayed on the left button.
        /// </summary>
        string DefaultLeftButtonContent { get; set; }

        /// <summary>
        /// Gets or sets the default content displayed on the right button.
        /// </summary>
        string DefaultRightButtonContent { get; set; }

        /// <summary>
        /// Gets or sets the default content displayed on the progress action button.
        /// </summary>
        string DefaultProgressButtonContent { get; set; }
    }
}
