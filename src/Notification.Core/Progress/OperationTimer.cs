using System;
using System.Diagnostics;

namespace Notification.Core
{
    /// <summary>
    /// Estimates the remaining time for a long-running operation based on elapsed time and current progress.
    /// </summary>
    public class OperationTimer : IDisposable
    {
        /// <summary>
        /// Gets or sets the message displayed while the initial time estimate is being calculated.
        /// </summary>
        public string BaseWaitingMessage { get; set; } = "Calculating time";

        private Stopwatch Watch { get; set; } = new Stopwatch();

        private double LastSpan;

        private bool IsRunning { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationTimer"/> class with a custom waiting message.
        /// </summary>
        /// <param name="waitingMessage">The message displayed while calculating the initial estimate.</param>
        public OperationTimer(string waitingMessage) => BaseWaitingMessage = waitingMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationTimer"/> class with the default waiting message.
        /// </summary>
        public OperationTimer() { }

        /// <summary>
        /// Starts or resumes the internal stopwatch.
        /// </summary>
        public void Start()
        {
            Watch?.Start();
            LastSpan = 0;
            IsRunning = true;
        }

        /// <summary>
        /// Stops the internal stopwatch.
        /// </summary>
        public void Stop()
        {
            Watch?.Stop();
            LastSpan = 0;
            IsRunning = false;
        }

        /// <summary>
        /// Resets the internal stopwatch and restarts timing from zero.
        /// </summary>
        public void Reset()
        {
            Watch?.Reset();
            LastSpan = 0;
            IsRunning = true;
        }

        /// <summary>
        /// Restarts the internal stopwatch, resetting the elapsed time to zero and starting immediately.
        /// </summary>
        public void Restart()
        {
            Watch?.Restart();
            LastSpan = 0;
            IsRunning = true;
        }

        /// <summary>
        /// Calculates the estimated remaining operation time based on integer progress indices.
        /// </summary>
        /// <param name="currentIndex">The current progress index (items completed).</param>
        /// <param name="totalIndex">The total number of items to process.</param>
        /// <returns>The estimated remaining time, or null if the estimate cannot be calculated yet.</returns>
        public TimeSpan? CalculateOperationTime(int currentIndex, int totalIndex) =>
            CalculateOperationTime((double)currentIndex, (double)totalIndex);

        /// <summary>
        /// Returns the estimated remaining time as a human-readable string based on integer progress indices.
        /// </summary>
        /// <param name="currentIndex">The current progress index (items completed).</param>
        /// <param name="totalIndex">The total number of items to process.</param>
        /// <returns>A formatted time string, or the base waiting message if the estimate is not yet available.</returns>
        public string GetStringTime(int currentIndex, int totalIndex) =>
            GetStringTime((double)currentIndex, (double)totalIndex);

        /// <summary>
        /// Calculates the estimated remaining operation time based on double progress values, using a smoothed average.
        /// </summary>
        /// <param name="currentIndex">The current progress value.</param>
        /// <param name="totalIndex">The total progress target value.</param>
        /// <returns>The estimated remaining time, or null if the estimate cannot be calculated yet.</returns>
        public TimeSpan? CalculateOperationTime(double currentIndex, double totalIndex)
        {
            if (Watch is null)
                return null;
            if (!IsRunning)
                Start();
            if (currentIndex == 0)
                return null;

            double midOperationTime = Watch.Elapsed.TotalSeconds / currentIndex;

            if (LastSpan == 0)
            {
                double remained = (totalIndex - currentIndex) * midOperationTime;
                LastSpan = remained;
                return TimeSpan.FromSeconds(remained);
            }
            else
            {
                double remained = (totalIndex - currentIndex) * midOperationTime;
                double midValue = (remained + LastSpan) / 2;
                LastSpan = midValue;
                return TimeSpan.FromSeconds(midValue);
            }
        }

        /// <summary>
        /// Returns the estimated remaining time as a human-readable string, formatted as days, hours, minutes, or seconds.
        /// </summary>
        /// <param name="currentIndex">The current progress value.</param>
        /// <param name="totalIndex">The total progress target value.</param>
        /// <returns>A formatted time string, or the base waiting message if the estimate is not yet available.</returns>
        public string GetStringTime(double currentIndex, double totalIndex)
        {
            TimeSpan? time = CalculateOperationTime(currentIndex, totalIndex);
            return time is null ? BaseWaitingMessage ?? "" :
                time.Value.Days > 0 ? time.Value.ToString(@"d\.hh\:mm\:ss") :
                time.Value.Hours > 0 ? time.Value.ToString(@"hh\:mm\:ss") :
                time.Value.Minutes > 0 ? time.Value.ToString(@"mm\:ss") :
                $"{Math.Round(time.Value.TotalSeconds, 0)} s";
        }

        /// <summary>
        /// Releases the internal stopwatch resources.
        /// </summary>
        public void Dispose() => Watch = null;
    }
}
