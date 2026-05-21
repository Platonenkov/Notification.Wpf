using System;
using System.Diagnostics;

namespace Notification.Core
{
    /// <summary>
    /// A progress reporter that throttles update notifications to prevent excessive UI refreshes.
    /// Reports are suppressed if they occur more frequently than the specified timeout interval.
    /// </summary>
    /// <typeparam name="T">The type of the progress value.</typeparam>
    public class SlowedProgress<T> : Progress<T>, IDisposable
    {
        private readonly int _updateTimeOut;
        private Stopwatch _watch = new Stopwatch();

        /// <summary>
        /// Initializes a new instance of the <see cref="SlowedProgress{T}"/> class with a handler and throttle interval.
        /// </summary>
        /// <param name="handler">The action invoked when a progress update is reported (subject to throttling).</param>
        /// <param name="updateTimeOut">The minimum interval in milliseconds between progress reports.</param>
        public SlowedProgress(Action<T> handler, int updateTimeOut) : base(handler)
        {
            _updateTimeOut = updateTimeOut;
            _watch.Start();
        }

        /// <summary>
        /// Reports a progress update, suppressing it if the throttle interval has not elapsed since the last report.
        /// </summary>
        /// <param name="value">The progress value to report.</param>
        protected override void OnReport(T value)
        {
            if (_watch.ElapsedMilliseconds <= _updateTimeOut)
                return;
            _watch.Restart();
            base.OnReport(value);
        }

        /// <summary>
        /// Releases the internal stopwatch resources.
        /// </summary>
        public void Dispose()
        {
            _watch = null;
        }
    }
}
