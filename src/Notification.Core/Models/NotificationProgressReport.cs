namespace Notification.Core
{
    /// <summary>
    /// Represents an immutable progress report for a notification progress bar.
    /// </summary>
    public readonly struct NotificationProgressReport
    {
        /// <summary>
        /// Gets the progress value as a percentage (0-100), or null if indeterminate.
        /// </summary>
        public double? Value { get; }

        /// <summary>
        /// Gets the progress status message displayed to the user.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the progress bar title text.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets a value indicating whether the cancel button should be shown, or null to keep the current state.
        /// </summary>
        public bool? ShowCancel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationProgressReport"/> struct.
        /// </summary>
        /// <param name="value">The progress value as a percentage (0-100), or null for indeterminate.</param>
        /// <param name="message">The progress status message.</param>
        /// <param name="title">The progress bar title.</param>
        /// <param name="showCancel">Whether to show the cancel button, or null to keep the current state.</param>
        public NotificationProgressReport(double? value, string message, string title, bool? showCancel)
        {
            Value = value;
            Message = message;
            Title = title;
            ShowCancel = showCancel;
        }
    }
}
