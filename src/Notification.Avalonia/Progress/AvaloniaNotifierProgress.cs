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

        public bool IsFinished => _isFinished;
        public CancellationToken CancellationToken => _handle.CancellationToken;
        public CancellationTokenSource CancelSource => _handle.CancelSource;
        public OperationTimer WaitingTimer => _waitingTimer;

        public AvaloniaNotifierProgress(ProgressCardHandle handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _throttleTimer.Start();
            _waitingTimer.Start();
        }

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
