using System;

namespace Notification.Core
{
    public class ProgressSelector<T, TSource> : IProgress<TSource>
    {
        private readonly IProgress<T> _progress;
        private readonly Func<TSource, T> _selector;

        public ProgressSelector(IProgress<T> progress, Func<TSource, T> selector)
        {
            _progress = progress;
            _selector = selector;
        }

        public void Report(TSource value) => _progress.Report(_selector(value));
    }

    public static class ProgressExtensions
    {
        public static IProgress<TSource> Select<TSource, T>(this IProgress<T> progress, Func<TSource, T> selector) =>
            progress is null ? null : new ProgressSelector<T, TSource>(progress, selector);
    }
}
