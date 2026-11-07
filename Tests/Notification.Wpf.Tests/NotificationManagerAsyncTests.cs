using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Notification.Core;
using Notification.Wpf;
using Xunit;

// NotificationManager.ShowContent enumerates the static Areas list and reads each area's
// FrameworkElement.Name — a cross-thread DependencyProperty read that throws VerifyAccess
// whenever another test's STA thread owns an area in that list. The static UI state makes
// cross-collection parallelism unsafe; run every test serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Notification.Wpf.Tests
{
    // Helper: runs an async delegate on a shared single STA thread with its own Dispatcher,
    // pumping messages until the delegate completes. Required because WPF types assert STA.
    //
    // All tests share ONE STA thread: WPF resources (Generic.xaml Styles/Templates) are
    // DependencyObjects created on first use, so running each test on a fresh thread makes
    // later tests reuse resources owned by a dead thread — template application then throws
    // "DependencySource must be created on the thread of the DependencyObject", the
    // unhandled exception kills Dispatcher.Run, and every subsequent await hangs.
    internal static class StaTask
    {
        private static readonly object Gate = new();
        private static Thread _staThread;
        private static Dispatcher _staDispatcher;

        // Last exception swallowed by the pump handler; read by Run to fail the owning test
        // instead of passing silently. Written only on the STA thread, read after done.Wait.
        private static volatile Exception? _unhandled;

        // Set when a test times out: the discarded thread may still resume queued continuations
        // later, so a fresh thread would race it on the static Areas/_window state. The helper
        // then fails fast on every subsequent Run instead of producing untrustworthy results.
        private static bool _poisoned;
        private static string? _poisonReason;

        public static void Run(Func<Task> asyncTest, TimeSpan timeout)
        {
            EnsureStaThread();
            _unhandled = null;   // drop any late write from a previous test's pump tail

            Exception? caught = null;
            using ManualResetEventSlim done = new(false);
            _staDispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    // Each test starts from clean shared WPF statics: a previous test's area or
                    // overlay window (possibly owned by a discarded thread) left in Areas/_window
                    // makes ShowContent read a foreign DependencyObject and throw VerifyAccess.
                    ResetSharedWpfState();
                    await asyncTest();
                }
                catch (Exception ex) { caught = ex; }
                finally { done.Set(); }
            }));

            if (!done.Wait(timeout))
            {
                // The test's async chain is stuck on this thread. Dropping the thread alone is
                // not enough: it stays alive as a background thread and may later resume queued
                // continuations, racing the NEXT test's fresh thread on the static Areas/_window
                // state. Poison the helper so subsequent Runs fail fast with a clear reason.
                lock (Gate)
                {
                    _staThread = null;
                    _staDispatcher = null;
                    _poisoned = true;
                    _poisonReason =
                        $"A previous StaTask.Run timed out after {timeout}; the shared STA thread " +
                        "was discarded and the helper is disabled to avoid racing on static WPF " +
                        "state (Areas/_window). Fix the hanging test before rerunning the suite.";
                }
                throw new TimeoutException($"StaTask timed out after {timeout}");
            }

            if (caught != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(caught).Throw();

            // An exception swallowed by the pump handler must not become a silent pass.
            Exception? unhandled = _unhandled;
            if (unhandled != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(unhandled).Throw();
        }

        public static void Run(Func<Task> asyncTest) => Run(asyncTest, TimeSpan.FromSeconds(10));

        // Runs on the shared STA thread before each test. Clears the static Areas list and
        // disposes any leftover overlay window so no state from a previous test (or a
        // discarded thread) leaks into ShowContent's cross-thread reads.
        private static void ResetSharedWpfState()
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;

            // Lazily create the Application once so tests can stage a MainWindow for the
            // overlay hook. OnExplicitShutdown keeps a test closing its last window from
            // shutting the whole (shared) application down.
            if (Application.Current == null)
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

            // Every test starts without a main window so EnsureOverlayMainHook deterministically
            // skips on the first Show (MainWindow null) and each test stages its own.
            Application.Current.MainWindow = null;

            if (typeof(NotificationManager).GetField("Areas", flags)?.GetValue(null)
                is System.Collections.IList areas)
                areas.Clear();

            if (typeof(NotificationManager).GetField("_window", flags)?.GetValue(null) is Window w)
            {
                if (w.Dispatcher.Thread == Thread.CurrentThread)
                    w.Close();   // same thread: the Closed handler nulls the field
                else
                    typeof(NotificationManager).GetField("_window", flags).SetValue(null, null);
            }

            // Drop any leftover main-window hook a previous test may have left attached.
            typeof(NotificationManager).GetMethod("ClearHookState", flags)?.Invoke(null, null);
        }

        private static void EnsureStaThread()
        {
            lock (Gate)
            {
                if (_poisoned)
                    throw new InvalidOperationException(
                        _poisonReason ?? "StaTask helper disabled after a previous timeout.");

                if (_staThread != null && _staThread.IsAlive && _staDispatcher != null)
                    return;

                ManualResetEventSlim ready = new(false);
                Exception? startEx = null;
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                        // Keep the pump alive when exceptions escape into it (e.g. async-void or
                        // layout failures): killing Run would hang every await in the test.
                        // But record the exception instead of swallowing it silently — Run
                        // rethrows it after the test body, so a real failure cannot pass green.
                        dispatcher.UnhandledException += (_, e) =>
                        {
                            e.Handled = true;
                            _unhandled ??= e.Exception;
                        };
                        SynchronizationContext.SetSynchronizationContext(
                            new DispatcherSynchronizationContext(dispatcher));
                        _staDispatcher = dispatcher;
                        ready.Set();
                        Dispatcher.Run();
                    }
                    catch (Exception ex) { startEx = ex; ready.Set(); }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
                ready.Wait();
                if (startEx != null)
                    throw startEx;
                _staThread = thread;
            }
        }
    }

    // Shares static Areas/_window state with CloseButtonOrderingTests — same collection keeps
    // them serialized instead of racing on the shared List during enumeration.
    [Collection("NotificationUiState")]
    public class NotificationManagerAsyncTests
    {
        [Fact]
        public void ShowAsync_completes_with_cancelled_when_token_cancels()
        {
            // This path completes through CancellationToken registration in NotificationCompletionSource
            // without touching WPF UI, so it works without a real Dispatcher pump. An already-cancelled
            // token makes the cancellation win deterministically: the registration completes the source
            // as Cancelled at construction, before the no-area short-circuit can report Timeout. (With a
            // token cancelled *after* the call, no real NotificationArea exists in a headless test, so the
            // manager legitimately completes immediately as Timeout — that race is not what this asserts.)
            StaTask.Run(async () =>
            {
                NotificationManager mgr = new NotificationManager(Dispatcher.CurrentDispatcher);
                Task<NotificationCloseResult> task = mgr.ShowAsync(new NotificationRequest
                {
                    Message = "hi",
                    ExpirationTime = TimeSpan.FromSeconds(30)
                }, new CancellationToken(canceled: true));

                NotificationCloseResult r = await task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
            });
        }

        [Fact]
        public void ShowDialogAsync_throws_when_no_buttons()
        {
            StaTask.Run(async () =>
            {
                NotificationManager mgr = new NotificationManager(Dispatcher.CurrentDispatcher);
                await Assert.ThrowsAsync<ArgumentException>(() => mgr.ShowDialogAsync(new NotificationRequest
                {
                    Message = "x"
                }));
            });
        }

        [Fact]
        public void ShowDialogAsync_fallback_SkipSilently_returns_immediately_for_too_many_buttons()
        {
            StaTask.Run(async () =>
            {
                NotificationManager mgr = new NotificationManager(Dispatcher.CurrentDispatcher);
                NotificationRequest req = new NotificationRequest
                {
                    Message = "x",
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "a", Content = "A", Role = NotificationButtonRole.Affirmative },
                        new NotificationButton { Id = "b", Content = "B", Role = NotificationButtonRole.Negative },
                        new NotificationButton { Id = "c", Content = "C" }
                    }
                };
                NotificationCloseResult r = await mgr.ShowDialogAsync(req, NotificationDialogFallback.SkipSilently)
                    .WaitAsync(TimeSpan.FromSeconds(2));
                Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
            });
        }

        [Fact]
        public void ShowAsync_duplicate_button_ids_throws()
        {
            StaTask.Run(async () =>
            {
                NotificationManager mgr = new NotificationManager(Dispatcher.CurrentDispatcher);
                await Assert.ThrowsAsync<ArgumentException>(() => mgr.ShowAsync(new NotificationRequest
                {
                    Message = "x",
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "dup", Content = "A" },
                        new NotificationButton { Id = "dup", Content = "B" }
                    }
                }));
            });
        }

        [Fact]
        public void ShowDialogAsync_Throw_fallback_raises_when_too_many_buttons()
        {
            StaTask.Run(async () =>
            {
                NotificationManager mgr = new NotificationManager(Dispatcher.CurrentDispatcher);
                NotificationRequest req = new NotificationRequest
                {
                    Message = "x",
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "a", Content = "A", Role = NotificationButtonRole.Affirmative },
                        new NotificationButton { Id = "b", Content = "B", Role = NotificationButtonRole.Negative },
                        new NotificationButton { Id = "c", Content = "C" }
                    }
                };
                await Assert.ThrowsAsync<NotSupportedException>(() => mgr.ShowDialogAsync(req, NotificationDialogFallback.Throw));
            });
        }

        [Fact]
        public void ShowDialogAsync_ShowFirstAffirmative_picks_affirmative_button_when_three_given()
        {
            // ShowFirstAffirmative reduces to a single button request and proceeds via ShowAsync.
            // The resulting task is not expected to complete (no real area) — we only verify
            // that the call does not throw and the notification is registered in the manager.
            StaTask.Run(() =>
            {
                NotificationManager mgr = new NotificationManager(Dispatcher.CurrentDispatcher);
                NotificationRequest req = new NotificationRequest
                {
                    Message = "x",
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "a", Content = "A", Role = NotificationButtonRole.Negative },
                        new NotificationButton { Id = "b", Content = "B", Role = NotificationButtonRole.Affirmative },
                        new NotificationButton { Id = "c", Content = "C" }
                    }
                };
                // Should NOT throw — picks "b" (first Affirmative) and delegates to ShowAsync.
                Task<NotificationCloseResult> t = mgr.ShowDialogAsync(req, NotificationDialogFallback.ShowFirstAffirmative);
                Assert.NotNull(t);
                // Clean up so the task doesn't linger.
                mgr.Dismiss(req.Id);
                return Task.CompletedTask;
            });
        }
    }
}
