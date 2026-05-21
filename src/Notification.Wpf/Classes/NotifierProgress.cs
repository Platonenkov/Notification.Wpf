using System;
using System.Threading;
using System.Windows;
using Notification.Core;
using Notification.Wpf.Extensions;

namespace Notification.Wpf.Classes
{
    public sealed class NotifierProgress<T> : Progress<T>, IDisposable
    {
        #region IsFinished : bool - progress was finished

        /// <summary>progress was finished</summary>
        public bool IsFinished => _IsFinished;

        /// <summary>progress was finished</summary>
        private bool _IsFinished;

        #endregion

        public readonly OperationTimer WaitingTimer = new();
        private Controls.Notification Area;
        private readonly CancellationTokenSource _CancelSource = new();
        public CancellationTokenSource CancelSource => _CancelSource;
        public CancellationToken Cancel => _CancelSource.Token;


        public NotifierProgress(Action<T> handler, CancellationTokenSource source) : base(handler) { _CancelSource = source; }
        public NotifierProgress(Action<T> handler) : base(handler) { }
        public void Report(T value) => base.OnReport(value);

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

        public void SetArea(Controls.Notification area)
        {
            Area = area;
        }

    }
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
    }
}
