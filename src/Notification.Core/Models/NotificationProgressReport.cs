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

        /// <summary>
        /// Initializes a new determinate progress report with only a value.
        /// </summary>
        /// <param name="value">The progress value as a percentage (0-100).</param>
        public NotificationProgressReport(double value)
            : this(value, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new determinate progress report with a value and a status message.
        /// </summary>
        /// <param name="value">The progress value as a percentage (0-100).</param>
        /// <param name="message">The progress status message.</param>
        public NotificationProgressReport(double value, string message)
            : this(value, message, null, null)
        {
        }

        /// <summary>
        /// Initializes a new determinate progress report with a value, a status message and a title.
        /// </summary>
        /// <param name="value">The progress value as a percentage (0-100).</param>
        /// <param name="message">The progress status message.</param>
        /// <param name="title">The progress bar title.</param>
        public NotificationProgressReport(double value, string message, string title)
            : this(value, message, title, null)
        {
        }

        /// <summary>
        /// Creates an indeterminate progress report that carries no numeric value.
        /// </summary>
        /// <param name="message">The progress status message.</param>
        /// <param name="title">The progress bar title.</param>
        /// <param name="showCancel">Whether to show the cancel button, or null to keep the current state.</param>
        /// <returns>An indeterminate <see cref="NotificationProgressReport"/>.</returns>
        public static NotificationProgressReport Indeterminate(string message = null, string title = null, bool? showCancel = null)
            => new NotificationProgressReport(null, message, title, showCancel);

        /// <summary>
        /// Returns a copy of this report with the specified progress value.
        /// </summary>
        /// <param name="value">The new progress value, or null for indeterminate.</param>
        /// <returns>A new <see cref="NotificationProgressReport"/> with the updated value.</returns>
        public NotificationProgressReport WithValue(double? value)
            => new NotificationProgressReport(value, Message, Title, ShowCancel);

        /// <summary>
        /// Returns a copy of this report with the specified status message.
        /// </summary>
        /// <param name="message">The new status message.</param>
        /// <returns>A new <see cref="NotificationProgressReport"/> with the updated message.</returns>
        public NotificationProgressReport WithMessage(string message)
            => new NotificationProgressReport(Value, message, Title, ShowCancel);

        /// <summary>
        /// Returns a copy of this report with the specified title.
        /// </summary>
        /// <param name="title">The new title.</param>
        /// <returns>A new <see cref="NotificationProgressReport"/> with the updated title.</returns>
        public NotificationProgressReport WithTitle(string title)
            => new NotificationProgressReport(Value, Message, title, ShowCancel);

        /// <summary>
        /// Returns a copy of this report with the specified cancel button visibility.
        /// </summary>
        /// <param name="showCancel">Whether to show the cancel button, or null to keep the current state.</param>
        /// <returns>A new <see cref="NotificationProgressReport"/> with the updated cancel button visibility.</returns>
        public NotificationProgressReport WithShowCancel(bool? showCancel)
            => new NotificationProgressReport(Value, Message, Title, showCancel);
    }
}
