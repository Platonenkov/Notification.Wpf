using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Notification.Wpf.Extensions
{
    internal static class Extensions
    {
        public static async Task<TResult> WhenAny<T, TResult>(
            this IEnumerable<T> items,
            Func<T, CancellationToken, Task<TResult>> Selector)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();

            IEnumerable<Task<TResult>> tasks = items.Select(item =>
            {
                Task<TResult> task = Selector(item, cancellation.Token);
                task.ContinueWith(_ => cancellation.Cancel(), TaskContinuationOptions.OnlyOnRanToCompletion);
                return task;
            }).TakeWhile(_ => !cancellation.IsCancellationRequested);

            Task<TResult> result = await Task.WhenAny(tasks).ConfigureAwait(false);
            return await result;
        }
    }
}
