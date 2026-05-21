using System;

namespace Notification.Core
{
    /// <summary>
    /// Adapts an <see cref="IProgress{T}"/> to accept a different source type by applying a selector function.
    /// </summary>
    /// <typeparam name="T">The target progress value type.</typeparam>
    /// <typeparam name="TSource">The source progress value type.</typeparam>
    public class ProgressSelector<T, TSource> : IProgress<TSource>
    {
        private readonly IProgress<T> _progress;
        private readonly Func<TSource, T> _selector;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressSelector{T, TSource}"/> class.
        /// </summary>
        /// <param name="progress">The target progress reporter to delegate to.</param>
        /// <param name="selector">A function that transforms the source value to the target type.</param>
        public ProgressSelector(IProgress<T> progress, Func<TSource, T> selector)
        {
            _progress = progress;
            _selector = selector;
        }

        /// <summary>
        /// Reports the source progress value after transforming it using the selector function.
        /// </summary>
        /// <param name="value">The source progress value.</param>
        public void Report(TSource value) => _progress.Report(_selector(value));
    }

    /// <summary>
    /// Provides extension methods for <see cref="IProgress{T}"/> to enable type projection.
    /// </summary>
    public static class ProgressExtensions
    {
        /// <summary>
        /// Projects the source progress type into the target progress type using a selector function.
        /// </summary>
        /// <typeparam name="TSource">The source progress value type.</typeparam>
        /// <typeparam name="T">The target progress value type.</typeparam>
        /// <param name="progress">The target progress reporter. If null, returns null.</param>
        /// <param name="selector">A function that transforms the source value to the target type.</param>
        /// <returns>An <see cref="IProgress{TSource}"/> that projects values to the target progress, or null if the input progress is null.</returns>
        public static IProgress<TSource> Select<TSource, T>(this IProgress<T> progress, Func<TSource, T> selector) =>
            progress is null ? null : new ProgressSelector<T, TSource>(progress, selector);
    }
}
