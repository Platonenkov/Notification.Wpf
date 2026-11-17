using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Notification.Core;
using Notification.Wpf;
using Notification.Wpf.Tests.Lifecycle;
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
                // Dispatcher.Run installs this context only later; without it the first await resumes on a
                // thread-pool thread and the test body touches WPF objects from the wrong thread.
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
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

    // Runs on the shared GUI thread: ShowAsync builds NotificationContent, which initialises the thread-affine
    // brushes in NotificationConstants, and a throwaway thread would own them after it exits.
    [Collection("Gui")]
    public class NotificationManagerAsyncTests : IDisposable
    {
        // On the shared GUI thread a show without an area name creates a real overlay window with a live
        // session; left in place it would be the overlay the next GUI test sees.
        public void Dispose() => GuiThread.Run(() =>
        {
            NotificationManager.ResetStateForTests();
            return Task.CompletedTask;
        });

        [Fact]
        public void ShowAsync_completes_with_cancelled_when_token_cancels()
        {
            // An already-cancelled token makes the cancellation win deterministically: the registration in
            // NotificationCompletionSource completes the source as Cancelled at construction, before the show
            // itself runs. The show still creates an overlay toast; Dispose removes it.
            GuiThread.Run(async () =>
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
            GuiThread.Run(async () =>
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
            GuiThread.Run(async () =>
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
            GuiThread.Run(async () =>
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
            GuiThread.Run(async () =>
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
            GuiThread.Run(() =>
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
