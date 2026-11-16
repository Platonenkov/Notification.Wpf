using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Notification.Wpf.Tests.Lifecycle
{
    /// <summary>
    /// Runs GUI tests on one process-wide STA dispatcher thread. The library keeps default brushes
    /// and similar objects in statics; they belong to the thread that created them, so a fresh thread
    /// per test would fail with cross-thread binding errors as soon as a second test renders a toast.
    /// Safe without extra locking because the "Gui" collection never runs in parallel.
    /// </summary>
    internal static class GuiThread
    {
        private static readonly Lazy<Dispatcher> SharedDispatcher = new Lazy<Dispatcher>(Start);

        public static void Run(Func<Task> asyncTest, TimeSpan? timeout = null)
        {
            // The async wrapper turns an exception thrown before the first await into a faulted task
            // instead of an unhandled dispatcher exception that would kill the shared thread.
            Task task = SharedDispatcher.Value.InvokeAsync(async () => await asyncTest()).Task.Unwrap();

            if (!task.Wait(timeout ?? TimeSpan.FromSeconds(10)))
                throw new TimeoutException("GuiThread test timed out.");

            task.GetAwaiter().GetResult();
        }

        private static Dispatcher Start()
        {
            Dispatcher? dispatcher = null;
            using ManualResetEventSlim ready = new ManualResetEventSlim(false);
            Thread thread = new Thread(() =>
            {
                dispatcher = Dispatcher.CurrentDispatcher;
                // Dispatcher.Run installs this context only later; without it the first await in a test
                // would resume on a thread-pool thread.
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                ready.Set();
                Dispatcher.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Name = "GuiThread";
            thread.Start();
            ready.Wait();
            return dispatcher!;
        }
    }
}
