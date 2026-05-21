using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Threading;
using Notification.Avalonia.Controls;
using Notification.Core;

namespace Notification.Avalonia.Progress
{
    /// <summary>
    /// Avalonia implementation of INotifierProgress.
    /// Updates the progress notification card and closes it on Dispose.
    /// </summary>
    public sealed class AvaloniaNotifierProgress : INotifierProgress
    {
        private readonly ProgressCardHandle _handle;
        private readonly OperationTimer _waitingTimer = new OperationTimer();
        private readonly Stopwatch _throttleTimer = new Stopwatch();

        private bool _isFinished;

        /// <summary>
        /// Gets a value indicating whether the progress operation has finished.
        /// </summary>
        public bool IsFinished => _isFinished;

        /// <summary>
        /// Gets the cancellation token signaled when the user cancels the progress operation.
        /// </summary>
        public CancellationToken CancellationToken => _handle.CancellationToken;

        /// <summary>
        /// Gets the cancellation token source backing the progress operation.
        /// </summary>
        public CancellationTokenSource CancelSource => _handle.CancelSource;

        /// <summary>
        /// Gets the timer that tracks elapsed time and computes the estimated remaining time.
        /// </summary>
        public OperationTimer WaitingTimer => _waitingTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaNotifierProgress"/> class.
        /// </summary>
        /// <param name="handle">The handle used to update the progress notification card.</param>
        public AvaloniaNotifierProgress(ProgressCardHandle handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _throttleTimer.Start();
            _waitingTimer.Start();
        }

        /// <summary>
        /// Reports a progress update to the notification card.
        /// </summary>
        /// <param name="value">The progress report containing value, message, title, and cancel visibility.</param>
        public void Report(NotificationProgressReport value)
        {
            if (_isFinished)
                return;

            // Throttle UI updates to every 100ms (except 0% and 100%)
            if (_throttleTimer.ElapsedMilliseconds < 100 && value.Value.HasValue
                && value.Value != 0 && value.Value != 100)
                return;

            _throttleTimer.Restart();

            _handle.UpdateProgress(value.Value, value.Message, value.Title, value.ShowCancel);

            // Update waiting time estimate
            if (value.Value.HasValue && value.Value > 10)
            {
                string waitText = _waitingTimer.GetStringTime(value.Value.Value, 100);
                _handle.UpdateWaitingTime(waitText);
            }
            else if (value.Value.HasValue)
            {
                _handle.UpdateWaitingTime(_waitingTimer.BaseWaitingMessage);
            }
        }

        /// <summary>
        /// Marks the progress operation as finished and closes the notification card.
        /// </summary>
        public void Dispose()
        {
            _isFinished = true;
            _throttleTimer.Stop();

            try
            {
                Dispatcher.UIThread.Post(() => _handle.Close());
                _waitingTimer.Dispose();
                CancelSource?.Dispose();
            }
            catch
            {
                // Ignored
            }
        }
    }
}
