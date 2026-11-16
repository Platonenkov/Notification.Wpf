using System;
using System.Threading.Tasks;
using System.Windows.Threading;

using Notification.Core;
using Notification.Wpf.Base;

using Xunit;

namespace Notification.Wpf.Tests.Lifecycle
{
    [Collection("Gui")]
    [Trait("Category", "Gui")]
    public class OverlayLifetimeTests
    {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        private static NotificationRequest CreateRequest(TimeSpan? expiry = null)
        {
            return new NotificationRequest
            {
                Title = "title",
                Message = "message",
                AreaName = string.Empty,
                ExpirationTime = expiry ?? TimeSpan.FromHours(1)
            };
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow + Wait;
            while (!condition() && DateTime.UtcNow < deadline)
                await Task.Delay(20);
        }

        private static NotificationManager CreateManager() => new NotificationManager(Dispatcher.CurrentDispatcher);

        [Fact]
        public void Overlay_LastSessionEnds_WindowCloses()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    Task<NotificationCloseResult> result = manager.ShowAsync(CreateRequest(TimeSpan.FromMilliseconds(300)));
                    NotificationsOverlayWindow window = NotificationManager.CurrentOverlayWindow;
                    Assert.NotNull(window);
                    Assert.True(window.IsVisible);

                    await result.WaitAsync(Wait);
                    await WaitUntilAsync(() => !window.IsLoaded);

                    // Closed, not hidden: a hidden overlay keeps the process alive (upstream issue #10).
                    Assert.False(window.IsLoaded);
                    Assert.False(window.IsVisible);
                    Assert.Null(NotificationManager.CurrentOverlayWindow);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_NextShowAfterClose_CreatesNewWindow()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    Task<NotificationCloseResult> first = manager.ShowAsync(CreateRequest(TimeSpan.FromMilliseconds(300)));
                    NotificationsOverlayWindow firstWindow = NotificationManager.CurrentOverlayWindow;
                    await first.WaitAsync(Wait);
                    await WaitUntilAsync(() => !firstWindow.IsLoaded);

                    Task<NotificationCloseResult> second = manager.ShowAsync(CreateRequest());
                    NotificationsOverlayWindow secondWindow = NotificationManager.CurrentOverlayWindow;

                    Assert.NotNull(secondWindow);
                    Assert.NotSame(firstWindow, secondWindow);
                    Assert.True(secondWindow.IsVisible);
                    Assert.False(second.IsCompleted);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_ShowDuringLastClose_KeepsToast()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationRequest firstRequest = CreateRequest();
                    Task<NotificationCloseResult> first = manager.ShowAsync(firstRequest);
                    NotificationsOverlayWindow window = NotificationManager.CurrentOverlayWindow;
                    await Task.Delay(100);

                    manager.Dismiss(firstRequest.Id);
                    // Inside the 400 ms closing animation: the first toast still counts as active.
                    await Task.Delay(100);
                    Task<NotificationCloseResult> second = manager.ShowAsync(CreateRequest());

                    await first.WaitAsync(Wait);
                    await Task.Delay(300);

                    Assert.Same(window, NotificationManager.CurrentOverlayWindow);
                    Assert.True(window.IsLoaded);
                    Assert.False(second.IsCompleted);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_CallbackCancelsSessions_ShowAsyncCompletes()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    Task<NotificationCloseResult> result = manager.ShowAsync(CreateRequest());
                    NotificationsOverlayWindow window = NotificationManager.CurrentOverlayWindow;
                    await Task.Delay(100);

                    NotificationManager.OnLastHostWindowClosed();
                    NotificationCloseResult closed = await result.WaitAsync(Wait);
                    await WaitUntilAsync(() => !window.IsLoaded);

                    Assert.Equal(NotificationCloseReason.Cancelled, closed.Reason);
                    Assert.False(window.IsLoaded);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }
    }
}
