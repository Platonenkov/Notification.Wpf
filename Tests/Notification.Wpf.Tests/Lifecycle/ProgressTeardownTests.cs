using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Notification.Core;
using Notification.Wpf.Classes;
using Notification.Wpf.Controls;

using Xunit;

using WpfNotification = Notification.Wpf.Controls.Notification;

namespace Notification.Wpf.Tests.Lifecycle
{
    [Collection("Gui")]
    [Trait("Category", "Gui")]
    public class ProgressTeardownTests
    {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        private static NotifierProgress<NotificationProgressReport> ShowProgress(GuiHost host)
        {
            return host.Manager.ShowProgressBar(new ProgressBarOptions { Title = "progress", AreaName = "gui" });
        }

        private static NotificationRequest CreateToast(string areaName)
        {
            return new NotificationRequest
            {
                Title = "title",
                Message = "message",
                AreaName = areaName,
                ExpirationTime = TimeSpan.FromHours(1)
            };
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow + Wait;
            while (!condition() && DateTime.UtcNow < deadline)
                await Task.Delay(20);
        }

        // The first notification shown in a process stays reachable until a second one is shown (WPF-internal
        // one-time state), so retention tests show and close two throwaway ones first.
        private static async Task WarmUpAsync(GuiHost host)
        {
            for (int i = 0; i < 2; i++)
            {
                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateToast("gui"));
                await Task.Delay(100);
                host.Area.ActiveSessions[0].RequestClose();
                await result.WaitAsync(Wait);
                await Task.Delay(100);
            }
        }

        [Fact]
        public void HostWindowClosed_ProgressLoopEnds_AndCancelProgressRuns()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                NotifierProgress<NotificationProgressReport> progress = ShowProgress(host);
                await Task.Delay(100);
                Assert.Single(host.Area.ActiveSessions);

                host.Window.Close();
                await WaitUntilAsync(() => host.Area.ActiveSessions.Count == 0);

                Assert.Empty(host.Area.ActiveSessions);
                Assert.True(progress.Cancel.IsCancellationRequested);
            });
        }

        [Fact]
        public void HostWindowClosed_ToastReportsCancelled()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateToast("gui"));
                await Task.Delay(100);

                host.Window.Close();
                NotificationCloseResult closed = await result.WaitAsync(Wait);

                Assert.Equal(NotificationCloseReason.Cancelled, closed.Reason);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<WeakReference> ShowToastAndCloseWindowAsync(GuiHost host)
        {
            Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateToast("gui"));
            await Task.Delay(100);
            WeakReference reference = new WeakReference(host.Area.ActiveSessions[0].Control);

            host.Window.Close();
            await result.WaitAsync(Wait);
            await Task.Delay(100);
            return reference;
        }

        [Fact]
        public void HostWindowClosed_NotificationIsCollected()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                await WarmUpAsync(host);

                WeakReference notification = await ShowToastAndCloseWindowAsync(host);

                await GcAssert.CollectedAsync(notification);
            });
        }

        [Fact]
        public void AreaUnloadedByTabSwitch_KeepsNotificationsOpen()
        {
            GuiThread.Run(async () =>
            {
                NotificationArea area = new NotificationArea { Name = "tab" };
                TabControl tabs = new TabControl();
                tabs.Items.Add(new TabItem { Header = "one", Content = area });
                tabs.Items.Add(new TabItem { Header = "two", Content = new TextBlock { Text = "other" } });
                Window window = new Window
                {
                    Left = -10000,
                    Top = 0,
                    Width = 400,
                    Height = 300,
                    ShowActivated = false,
                    ShowInTaskbar = false,
                    Content = tabs
                };

                try
                {
                    window.Show();
                    await WaitUntilAsync(() => area.IsLoaded);
                    NotificationManager manager = new NotificationManager(window.Dispatcher);

                    Task<NotificationCloseResult> result = manager.ShowAsync(CreateToast("tab"));
                    await Task.Delay(100);
                    NotificationSession session = Assert.Single(area.ActiveSessions);

                    tabs.SelectedIndex = 1;
                    await WaitUntilAsync(() => !area.IsLoaded);
                    Assert.False(area.IsLoaded);
                    tabs.SelectedIndex = 0;
                    await WaitUntilAsync(() => area.IsLoaded);
                    await Task.Delay(100);

                    Assert.True(area.IsLoaded);
                    Assert.False(session.IsClosing);
                    Assert.Single(area.ActiveSessions);

                    session.RequestClose();
                    await result.WaitAsync(Wait);
                }
                finally
                {
                    window.Close();
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<(NotifierProgress<NotificationProgressReport> Progress, WeakReference Control)> ShowProgressAndCloseAsync(GuiHost host)
        {
            NotifierProgress<NotificationProgressReport> progress = ShowProgress(host);
            await Task.Delay(100);
            NotificationSession session = host.Area.ActiveSessions[0];
            WeakReference reference = new WeakReference(session.Control);
            session.RequestClose();
            session = null!;
            await WaitUntilAsync(() => host.Area.ActiveSessions.Count == 0);
            await Task.Delay(100);
            return (progress, reference);
        }

        [Fact]
        public void ProgressClosed_NotifierProgressNoLongerHoldsControl()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                await WarmUpAsync(host);

                (NotifierProgress<NotificationProgressReport> progress, WeakReference control) = await ShowProgressAndCloseAsync(host);

                await GcAssert.CollectedAsync(control);
                GC.KeepAlive(progress);
            });
        }

        [Fact]
        public void ProgressDispose_ClosesNotificationOnce()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                NotifierProgress<NotificationProgressReport> progress = ShowProgress(host);
                await Task.Delay(100);
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                int closedCount = 0;
                control.NotificationClosed += (_, _) => closedCount++;

                progress.Dispose();
                await WaitUntilAsync(() => host.Area.ActiveSessions.Count == 0);
                // The polling loop wakes up within a second and must not close it a second time.
                await Task.Delay(1500);

                Assert.Equal(1, closedCount);
            }, TimeSpan.FromSeconds(20));
        }
    }
}
