using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Notification.Core;
using Notification.Wpf.Base;
using Notification.Wpf.Classes;
using Notification.Wpf.Controls;

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
                    Assert.Single(((NotificationArea)secondWindow.Content).ActiveSessions);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_ShowInOverlayCloseGap_LandsInNewArea()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    _ = manager.ShowAsync(CreateRequest());
                    NotificationsOverlayWindow firstWindow = NotificationManager.CurrentOverlayWindow;
                    NotificationArea firstArea = (NotificationArea)firstWindow.Content;

                    // Closes the overlay the way the last released session does. The old area leaves the registry
                    // only on Unloaded, which WPF raises later, so a show right now used to be routed to it too.
                    firstArea.ReleaseSession(firstArea.ActiveSessions[0]);
                    NotificationManager.OnSessionReleased(firstArea);
                    Task<NotificationCloseResult> second = manager.ShowAsync(CreateRequest());

                    NotificationsOverlayWindow secondWindow = NotificationManager.CurrentOverlayWindow;
                    Assert.NotNull(secondWindow);
                    Assert.NotSame(firstWindow, secondWindow);
                    Assert.False(second.IsCompleted);
                    Assert.Single(((NotificationArea)secondWindow.Content).ActiveSessions);
                    Assert.Empty(firstArea.ActiveSessions);
                    await Task.CompletedTask;
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_HostGoneWithEmptyOverlay_ClosesOverlay()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationManager.HostWindowSource = () => new List<Window>();
                    _ = manager.ShowAsync(CreateRequest());
                    NotificationsOverlayWindow window = NotificationManager.CurrentOverlayWindow;
                    NotificationArea area = (NotificationArea)window.Content;

                    // An overlay left without a session (a handler failed in an earlier teardown) must not stay open.
                    area.ReleaseSession(area.ActiveSessions[0]);
                    NotificationManager.OnLastHostWindowClosed();

                    Assert.Null(NotificationManager.CurrentOverlayWindow);
                    await Task.CompletedTask;
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_OnCloseThrows_WindowStillCloses()
        {
            GuiThread.Run(async () =>
            {
                using DispatcherExceptionCapture unhandled = new DispatcherExceptionCapture();
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationRequest request = CreateRequest(TimeSpan.FromMilliseconds(200));
                    request.OnClose = () => throw new InvalidOperationException("on-close");
                    Task<NotificationCloseResult> result = manager.ShowAsync(request);
                    NotificationsOverlayWindow window = NotificationManager.CurrentOverlayWindow;

                    await result.WaitAsync(Wait);
                    await WaitUntilAsync(() => !window.IsLoaded);

                    // An empty overlay would keep the process alive after the last host window closes (PR #89).
                    Assert.Null(NotificationManager.CurrentOverlayWindow);
                    Assert.False(window.IsLoaded);
                    Assert.Contains(unhandled.Exceptions, ex => ex.Message == "on-close");
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

        [Fact]
        public void Overlay_ShowAfterHostGone_IsRefusedAndCompletesCancelled()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationManager.HostWindowSource = () => new List<Window>();
                    Task<NotificationCloseResult> first = manager.ShowAsync(CreateRequest());
                    NotificationsOverlayWindow window = NotificationManager.CurrentOverlayWindow;
                    NotificationArea area = (NotificationArea)window.Content;
                    await Task.Delay(100);

                    NotificationManager.OnLastHostWindowClosed();
                    int sessionsBefore = area.ActiveSessions.Count;

                    // Inside the closing animation of the first toast: the window the producer race used.
                    Task<NotificationCloseResult> second = manager.ShowAsync(CreateRequest());

                    Assert.True(second.IsCompleted);
                    Assert.Equal(NotificationCloseReason.Cancelled, (await second).Reason);
                    Assert.Equal(sessionsBefore, area.ActiveSessions.Count);

                    await first.WaitAsync(Wait);
                    await WaitUntilAsync(() => !window.IsLoaded);
                    Assert.Null(NotificationManager.CurrentOverlayWindow);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_ShowAfterHostGoneWithoutOverlay_CreatesNoWindow()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationManager.HostWindowSource = () => new List<Window>();
                    NotificationManager.OnLastHostWindowClosed();

                    manager.Show("title", "message");
                    Task<NotificationCloseResult> result = manager.ShowAsync(CreateRequest());

                    Assert.Null(NotificationManager.CurrentOverlayWindow);
                    Assert.True(result.IsCompleted);
                    Assert.Equal(NotificationCloseReason.Cancelled, (await result).Reason);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_RefusedShow_DoesNotInvokeOnClose()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationManager.HostWindowSource = () => new List<Window>();
                    NotificationManager.OnLastHostWindowClosed();
                    int closed = 0;
                    NotificationRequest request = CreateRequest();
                    request.OnClose = () => closed++;

                    manager.Show(request);
                    await Task.Delay(200);

                    Assert.Equal(0, closed);
                    Assert.Null(NotificationManager.CurrentOverlayWindow);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_ProgressAfterHostGone_EndsAsCancelled()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationManager.HostWindowSource = () => new List<Window>();
                    NotificationManager.OnLastHostWindowClosed();

                    NotifierProgress<NotificationProgressReport> progress = manager.ShowProgressBar(new ProgressBarOptions());

                    Assert.True(progress.Cancel.IsCancellationRequested);
                    Assert.Null(NotificationManager.CurrentOverlayWindow);
                    await Task.CompletedTask;
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_HostWindowAppearsAgain_LiftsRefusal()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationManager.OnLastHostWindowClosed();
                    Window host = new Window();
                    NotificationManager.HostWindowSource = () => new List<Window> { host };

                    Task<NotificationCloseResult> result = manager.ShowAsync(CreateRequest());

                    Assert.NotNull(NotificationManager.CurrentOverlayWindow);
                    Assert.False(result.IsCompleted);

                    // The refusal stays lifted once a show found a host, without asking the source again.
                    NotificationManager.HostWindowSource = () => new List<Window>();
                    Task<NotificationCloseResult> next = manager.ShowAsync(CreateRequest());
                    Assert.False(next.IsCompleted);
                    await Task.CompletedTask;
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_ResetStateForTests_ClearsRefusal()
        {
            GuiThread.Run(async () =>
            {
                try
                {
                    NotificationManager manager = CreateManager();
                    NotificationManager.OnLastHostWindowClosed();
                    NotificationManager.ResetStateForTests();

                    Task<NotificationCloseResult> result = manager.ShowAsync(CreateRequest());

                    Assert.NotNull(NotificationManager.CurrentOverlayWindow);
                    Assert.False(result.IsCompleted);
                    await Task.CompletedTask;
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [Fact]
        public void Overlay_HostGone_NamedAreaStillShows()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync("RefusalArea");
                try
                {
                    NotificationManager.OnLastHostWindowClosed();
                    NotificationRequest request = CreateRequest();
                    request.AreaName = "RefusalArea";

                    Task<NotificationCloseResult> result = host.Manager.ShowAsync(request);

                    Assert.False(result.IsCompleted);
                    Assert.Single(host.Area.ActiveSessions);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }
    }
}
