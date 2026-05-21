using System;
using System.Threading;
using System.Windows;
using Notification.Core;
using Notification.Wpf.Extensions;

namespace Notification.Wpf.Classes
{
    /// <summary>
    /// Provides progress reporting for a notification operation with cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of the progress report value.</typeparam>
    public sealed class NotifierProgress<T> : Progress<T>, IDisposable
    {
        #region IsFinished : bool - progress was finished

        /// <summary>progress was finished</summary>
        public bool IsFinished => _IsFinished;

        /// <summary>progress was finished</summary>
        private bool _IsFinished;

        #endregion

        /// <summary>Timer used to measure the elapsed time of the operation.</summary>
        public readonly OperationTimer WaitingTimer = new();
        private Controls.Notification Area;
        private readonly CancellationTokenSource _CancelSource = new();

        /// <summary>Gets the cancellation token source associated with the operation.</summary>
        public CancellationTokenSource CancelSource => _CancelSource;

        /// <summary>Gets the cancellation token of the operation.</summary>
        public CancellationToken Cancel => _CancelSource.Token;


        /// <summary>
        /// Initializes a new instance of the <see cref="NotifierProgress{T}"/> class with the specified handler and cancellation source.
        /// </summary>
        /// <param name="handler">The callback invoked for each reported progress value.</param>
        /// <param name="source">The cancellation token source associated with the operation.</param>
        public NotifierProgress(Action<T> handler, CancellationTokenSource source) : base(handler) { _CancelSource = source; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifierProgress{T}"/> class with the specified handler.
        /// </summary>
        /// <param name="handler">The callback invoked for each reported progress value.</param>
        public NotifierProgress(Action<T> handler) : base(handler) { }

        /// <summary>
        /// Reports a progress update.
        /// </summary>
        /// <param name="value">The value of the progress update.</param>
        public void Report(T value) => base.OnReport(value);

        /// <summary>
        /// Marks the progress as finished, closes the associated notification and releases all resources.
        /// </summary>
        public void Dispose()
        {
            _IsFinished = true;
            try
            {
                Application.Current?.Dispatcher.Invoke(() => Area?.Close());
                WaitingTimer.Dispose();
                _CancelSource?.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Associates the notification area that should be closed when the progress is disposed.
        /// </summary>
        /// <param name="area">The notification control representing the progress area.</param>
        public void SetArea(Controls.Notification area)
        {
            Area = area;
        }

    }

    /// <summary>
    /// Provides extension methods for converting notification progress into strongly typed progress instances.
    /// </summary>
    public static class ProgressExtensions
    {
        #region Sub operations

        private static double ChooseValueDouble(double value, bool? MultyOrDeVide100 = default)
            => MultyOrDeVide100 is null ? value : MultyOrDeVide100 is true ? value * 100 : value / 100;

        private static int ChooseValueInt(int value, bool? MultyOrDeVide100 = default)
            => MultyOrDeVide100 is null ? value : MultyOrDeVide100 is true ? value * 100 : value / 100;

        #endregion

        /// <summary>
        /// Get progress of type T: double, (double,string), (double,string,string), int, (int,string), (int,string,string)
        /// </summary>
        /// <typeparam name="T">The supported progress value type.</typeparam>
        /// <param name="progress">The underlying notification progress to wrap.</param>
        /// <param name="showCancel">Whether the cancel button should be shown; <see langword="null"/> keeps the default.</param>
        /// <param name="MultyOrDeVide100"><see langword="true"/> multiplies the value by 100, <see langword="false"/> divides by 100, <see langword="null"/> keeps it unchanged.</param>
        /// <returns>A strongly typed progress instance forwarding values to the notification progress.</returns>
        /// <exception cref="NotSupportedException">Thrown when <typeparamref name="T"/> is not a supported progress type.</exception>
        [Obsolete("Tuple-based progress is deprecated. Report a NotificationProgressReport directly (see Report(value, message, title) overloads in ProgressReportExtensions), or use GetValueProgress for plain double progress.")]
        public static IProgress<T> GetProgress<T>(this IProgress<NotificationProgressReport> progress, bool? showCancel, bool? MultyOrDeVide100 = default)
        {
            if (typeof(T) == typeof(double))
                return (IProgress<T>)progress.Select<double, NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueDouble(p, MultyOrDeVide100), null, null, showCancel));

            if (typeof(T) == typeof((double, string)))
                return (IProgress<T>)progress.Select<(double, string), NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueDouble(p.Item1, MultyOrDeVide100), p.Item2, null, showCancel));
            if (typeof(T) == typeof((double, string, string)))
                return (IProgress<T>)progress.Select<(double, string, string), NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueDouble(p.Item1, MultyOrDeVide100), p.Item2, p.Item3, showCancel));
            if (typeof(T) == typeof(int))
                return (IProgress<T>)progress.Select<int, NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueInt(p, MultyOrDeVide100), null, null, showCancel));
            if (typeof(T) == typeof((int, string)))
                return (IProgress<T>)progress.Select<(int, string), NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueInt(p.Item1, MultyOrDeVide100), p.Item2, null, showCancel));
            if (typeof(T) == typeof((int, string, string)))
                return (IProgress<T>)progress.Select<(int, string, string), NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueInt(p.Item1, MultyOrDeVide100), p.Item2, p.Item3, showCancel));

            throw new NotSupportedException("type of progress not supported");
        }

        /// <summary>
        /// Get progress of type T with throttling: double, (double,string), (double,string,string), int, (int,string), (int,string,string)
        /// </summary>
        /// <typeparam name="T">The supported progress value type.</typeparam>
        /// <param name="progress">The underlying notification progress to wrap.</param>
        /// <param name="showCancel">Whether the cancel button should be shown; <see langword="null"/> keeps the default.</param>
        /// <param name="MultyOrDeVide100"><see langword="true"/> multiplies the value by 100, <see langword="false"/> divides by 100, <see langword="null"/> keeps it unchanged.</param>
        /// <param name="UpdateTimeOut">The minimum interval, in milliseconds, between progress updates.</param>
        /// <returns>A throttled strongly typed progress instance forwarding values to the notification progress.</returns>
        /// <exception cref="NotSupportedException">Thrown when <typeparamref name="T"/> is not a supported progress type.</exception>
        [Obsolete("Tuple-based progress is deprecated. Use GetSlowedProgress(int) for throttled NotificationProgressReport, or GetSlowedValueProgress for throttled plain double progress.")]
        public static IProgress<T> GetSlowedProgress<T>(
            this IProgress<NotificationProgressReport> progress,
            bool? showCancel,
            bool? MultyOrDeVide100 = default,
            int UpdateTimeOut = 50)
        {
            if (typeof(T) == typeof(double))
                return (IProgress<T>)new SlowedProgress<double>(d => progress.Select<double, NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueDouble(p, MultyOrDeVide100), null, null, showCancel)).Report(d), UpdateTimeOut);

            if (typeof(T) == typeof((double, string)))
                return (IProgress<T>)new SlowedProgress<(double, string)>(d => progress.Select<(double, string), NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueDouble(p.Item1, MultyOrDeVide100), p.Item2, null, showCancel)).Report((d.Item1, d.Item2)), UpdateTimeOut);
            if (typeof(T) == typeof((double, string, string)))
                return (IProgress<T>)new SlowedProgress<(double, string, string)>(d => progress.Select<(double, string, string), NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueDouble(p.Item1, MultyOrDeVide100), p.Item2, p.Item3, showCancel)).Report((d.Item1, d.Item2, d.Item3)), UpdateTimeOut);
            if (typeof(T) == typeof(int))
                return (IProgress<T>)new SlowedProgress<int>(d => progress.Select<int, NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueInt(p, MultyOrDeVide100), null, null, showCancel)).Report(d), UpdateTimeOut);
            if (typeof(T) == typeof((int, string)))
                return (IProgress<T>)new SlowedProgress<(int, string)>(d => progress.Select<(int, string), NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueInt(p.Item1, MultyOrDeVide100), p.Item2, null, showCancel)).Report((d.Item1, d.Item2)), UpdateTimeOut);
            if (typeof(T) == typeof((int, string, string)))
                return (IProgress<T>)new SlowedProgress<(int, string, string)>(d => progress.Select<(int, string, string), NotificationProgressReport>(
                    p => new NotificationProgressReport(ChooseValueInt(p.Item1, MultyOrDeVide100), p.Item2, p.Item3, showCancel)).Report((d.Item1, d.Item2, d.Item3)), UpdateTimeOut);

            throw new NotSupportedException("type of progress not supported");
        }

        /// <summary>
        /// Wraps the notification progress so that updates are throttled to at most one per
        /// <paramref name="updateTimeOut"/> milliseconds.
        /// </summary>
        /// <param name="progress">The underlying notification progress to wrap.</param>
        /// <param name="updateTimeOut">The minimum interval, in milliseconds, between forwarded updates.</param>
        /// <returns>A throttled progress reporter, or <see langword="null"/> when <paramref name="progress"/> is null.</returns>
        public static IProgress<NotificationProgressReport> GetSlowedProgress(
            this IProgress<NotificationProgressReport> progress,
            int updateTimeOut = 50)
            => progress is null
                ? null
                : new SlowedProgress<NotificationProgressReport>(r => progress.Report(r), updateTimeOut);

        /// <summary>
        /// Adapts the notification progress to accept plain <see cref="double"/> values.
        /// </summary>
        /// <param name="progress">The underlying notification progress to wrap.</param>
        /// <param name="showCancel">Whether the cancel button should be shown; <see langword="null"/> keeps the default.</param>
        /// <param name="scale"><see langword="true"/> multiplies the value by 100, <see langword="false"/> divides by 100, <see langword="null"/> keeps it unchanged.</param>
        /// <returns>An <see cref="IProgress{T}"/> of <see cref="double"/> forwarding values to the notification progress, or <see langword="null"/> when <paramref name="progress"/> is null.</returns>
        public static IProgress<double> GetValueProgress(
            this IProgress<NotificationProgressReport> progress,
            bool? showCancel = null,
            bool? scale = null)
            => progress.Select<double, NotificationProgressReport>(
                p => new NotificationProgressReport(ChooseValueDouble(p, scale), null, null, showCancel));

        /// <summary>
        /// Adapts the notification progress to accept plain <see cref="double"/> values with throttled updates.
        /// </summary>
        /// <param name="progress">The underlying notification progress to wrap.</param>
        /// <param name="showCancel">Whether the cancel button should be shown; <see langword="null"/> keeps the default.</param>
        /// <param name="scale"><see langword="true"/> multiplies the value by 100, <see langword="false"/> divides by 100, <see langword="null"/> keeps it unchanged.</param>
        /// <param name="updateTimeOut">The minimum interval, in milliseconds, between forwarded updates.</param>
        /// <returns>A throttled <see cref="IProgress{T}"/> of <see cref="double"/>, or <see langword="null"/> when <paramref name="progress"/> is null.</returns>
        public static IProgress<double> GetSlowedValueProgress(
            this IProgress<NotificationProgressReport> progress,
            bool? showCancel = null,
            bool? scale = null,
            int updateTimeOut = 50)
            => progress is null
                ? null
                : new SlowedProgress<double>(
                    d => progress.Report(new NotificationProgressReport(ChooseValueDouble(d, scale), null, null, showCancel)),
                    updateTimeOut);
    }
}
