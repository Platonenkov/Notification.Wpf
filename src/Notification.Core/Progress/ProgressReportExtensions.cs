using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides convenience extension methods for reporting <see cref="NotificationProgressReport"/>
    /// values without constructing the struct manually.
    /// </summary>
    public static class ProgressReportExtensions
    {
        /// <summary>
        /// Reports a determinate progress update with only a value.
        /// </summary>
        /// <param name="progress">The progress reporter. The call is ignored when null.</param>
        /// <param name="value">The progress value as a percentage (0-100).</param>
        public static void Report(this IProgress<NotificationProgressReport> progress, double value)
            => progress?.Report(new NotificationProgressReport(value, null, null, null));

        /// <summary>
        /// Reports a determinate progress update with a value and a status message.
        /// </summary>
        /// <param name="progress">The progress reporter. The call is ignored when null.</param>
        /// <param name="value">The progress value as a percentage (0-100).</param>
        /// <param name="message">The progress status message.</param>
        public static void Report(this IProgress<NotificationProgressReport> progress, double value, string message)
            => progress?.Report(new NotificationProgressReport(value, message, null, null));

        /// <summary>
        /// Reports a determinate progress update with a value, a status message and a title.
        /// </summary>
        /// <param name="progress">The progress reporter. The call is ignored when null.</param>
        /// <param name="value">The progress value as a percentage (0-100).</param>
        /// <param name="message">The progress status message.</param>
        /// <param name="title">The progress bar title.</param>
        public static void Report(this IProgress<NotificationProgressReport> progress, double value, string message, string title)
            => progress?.Report(new NotificationProgressReport(value, message, title, null));

        /// <summary>
        /// Reports a determinate progress update with a value, a status message, a title and cancel button visibility.
        /// </summary>
        /// <param name="progress">The progress reporter. The call is ignored when null.</param>
        /// <param name="value">The progress value as a percentage (0-100).</param>
        /// <param name="message">The progress status message.</param>
        /// <param name="title">The progress bar title.</param>
        /// <param name="showCancel">Whether to show the cancel button, or null to keep the current state.</param>
        public static void Report(this IProgress<NotificationProgressReport> progress, double value, string message, string title, bool? showCancel)
            => progress?.Report(new NotificationProgressReport(value, message, title, showCancel));

        /// <summary>
        /// Reports an indeterminate progress update that carries no numeric value.
        /// </summary>
        /// <param name="progress">The progress reporter. The call is ignored when null.</param>
        /// <param name="message">The progress status message.</param>
        /// <param name="title">The progress bar title.</param>
        /// <param name="showCancel">Whether to show the cancel button, or null to keep the current state.</param>
        public static void ReportIndeterminate(
            this IProgress<NotificationProgressReport> progress,
            string message = null,
            string title = null,
            bool? showCancel = null)
            => progress?.Report(new NotificationProgressReport(null, message, title, showCancel));
    }
}
