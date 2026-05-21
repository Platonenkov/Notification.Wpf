namespace Notification.Core
{
    /// <summary>
    /// Represents configuration options for a progress bar notification.
    /// </summary>
    public class ProgressBarOptions
    {
        /// <summary>
        /// Gets or sets the progress bar title text.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cancel button is shown.
        /// </summary>
        public bool ShowCancelButton { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the progress bar is visible.
        /// </summary>
        public bool ShowProgress { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the notification area where the progress bar is displayed.
        /// </summary>
        public string AreaName { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether long text should be trimmed.
        /// </summary>
        public bool TrimText { get; set; }

        /// <summary>
        /// Gets or sets the default number of visible text rows.
        /// </summary>
        public uint DefaultRowsCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the base message displayed while calculating estimated remaining time.
        /// </summary>
        public string BaseWaitingMessage { get; set; } = "Calculation time";

        /// <summary>
        /// Gets or sets a value indicating whether the progress bar can be collapsed.
        /// </summary>
        public bool IsCollapse { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the title is shown when the progress bar is collapsed.
        /// </summary>
        public bool TitleWhenCollapsed { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom background color for the progress bar notification.
        /// </summary>
        public NotificationColor? BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the custom foreground (text) color for the progress bar notification.
        /// </summary>
        public NotificationColor? ForegroundColor { get; set; }

        /// <summary>
        /// Gets or sets the color of the progress indicator.
        /// </summary>
        public NotificationColor? ProgressColor { get; set; }

        /// <summary>
        /// Gets or sets the icon displayed in the progress bar notification. The type is platform-dependent.
        /// </summary>
        public object Icon { get; set; }

        /// <summary>
        /// Gets or sets the text rendering settings for the title.
        /// </summary>
        public NotificationTextSettings TitleSettings { get; set; }

        /// <summary>
        /// Gets or sets the text rendering settings for the message.
        /// </summary>
        public NotificationTextSettings MessageSettings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the close button is visible.
        /// </summary>
        public bool ShowCloseButton { get; set; } = true;
    }
}
