using System;
using System.Threading.Tasks;
using System.Windows.Threading;

using Xunit;

namespace Notification.Wpf.Tests.Lifecycle
{
    /// <summary>
    /// Collectability assertions. Callers must create the weak reference in a separate
    /// <c>[MethodImpl(MethodImplOptions.NoInlining)]</c> method, otherwise a JIT-extended
    /// local keeps the target alive. Results are reliable only in Release builds.
    /// </summary>
    internal static class GcAssert
    {
        private const int Rounds = 3;

        public static async Task CollectedAsync(WeakReference reference)
        {
            await CollectAsync();
            Assert.False(reference.IsAlive, "Expected the object to be garbage collected.");
        }

        public static async Task NotCollectedAsync(WeakReference reference)
        {
            await CollectAsync();
            Assert.True(reference.IsAlive, "Expected the object to stay alive.");
        }

        private static async Task CollectAsync()
        {
            // The idle yield lets pending dispatcher work (queued unloads, deferred releases)
            // drain before each collection, since it can hold the last reference.
            for (int i = 0; i < Rounds; i++)
            {
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
    }
}
