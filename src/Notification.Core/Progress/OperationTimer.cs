using System;
using System.Diagnostics;

namespace Notification.Core
{
    public class OperationTimer : IDisposable
    {
        public string BaseWaitingMessage { get; set; } = "Calculating time";

        private Stopwatch Watch { get; set; } = new Stopwatch();

        private double LastSpan;

        private bool IsRunning { get; set; }

        public OperationTimer(string waitingMessage) => BaseWaitingMessage = waitingMessage;
        public OperationTimer() { }

        public void Start()
        {
            Watch?.Start();
            LastSpan = 0;
            IsRunning = true;
        }

        public void Stop()
        {
            Watch?.Stop();
            LastSpan = 0;
            IsRunning = false;
        }

        public void Reset()
        {
            Watch?.Reset();
            LastSpan = 0;
            IsRunning = true;
        }

        public void Restart()
        {
            Watch?.Restart();
            LastSpan = 0;
            IsRunning = true;
        }

        public TimeSpan? CalculateOperationTime(int currentIndex, int totalIndex) =>
            CalculateOperationTime((double)currentIndex, (double)totalIndex);

        public string GetStringTime(int currentIndex, int totalIndex) =>
            GetStringTime((double)currentIndex, (double)totalIndex);

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

        public string GetStringTime(double currentIndex, double totalIndex)
        {
            TimeSpan? time = CalculateOperationTime(currentIndex, totalIndex);
            return time is null ? BaseWaitingMessage ?? "" :
                time.Value.Days > 0 ? time.Value.ToString(@"d\.hh\:mm\:ss") :
                time.Value.Hours > 0 ? time.Value.ToString(@"hh\:mm\:ss") :
                time.Value.Minutes > 0 ? time.Value.ToString(@"mm\:ss") :
                $"{Math.Round(time.Value.TotalSeconds, 0)} s";
        }

        public void Dispose() => Watch = null;
    }
}
