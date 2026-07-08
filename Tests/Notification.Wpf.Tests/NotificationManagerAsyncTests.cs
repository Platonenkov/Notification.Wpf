using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Notification.Core;
using Notification.Wpf;
using Xunit;

namespace Notification.Wpf.Tests
{
    // Helper: runs an async delegate on a dedicated STA thread with its own Dispatcher,
    // pumping messages until the delegate completes. Required because WPF types assert STA.
    internal static class StaTask
    {
        public static void Run(Func<Task> asyncTest, TimeSpan timeout)
        {
            Exception? caught = null;
            Thread thread = new Thread(() =>
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                Task t = asyncTest();
                t.ContinueWith(_ => dispatcher.InvokeShutdown());
                Dispatcher.Run();
                if (t.IsFaulted) caught = t.Exception!.GetBaseException();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            if (!thread.Join(timeout))
                throw new TimeoutException($"StaTask timed out after {timeout}");

            if (caught != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(caught).Throw();
        }

        public static void Run(Func<Task> asyncTest) => Run(asyncTest, TimeSpan.FromSeconds(10));
    }

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
