using System;
using System.Diagnostics;

namespace Notification.Core
{
    public class SlowedProgress<T> : Progress<T>, IDisposable
    {
        private readonly int _updateTimeOut;
        private Stopwatch _watch = new Stopwatch();

        public SlowedProgress(Action<T> handler, int updateTimeOut) : base(handler)
        {
            _updateTimeOut = updateTimeOut;
            _watch.Start();
        }

        protected override void OnReport(T value)
        {
            if (_watch.ElapsedMilliseconds <= _updateTimeOut)
                return;
            _watch.Restart();
            base.OnReport(value);
        }

        public void Dispose()
        {
            _watch = null;
        }
    }
}
