using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Notification.Core;
using Notification.Wpf;
using Notification.Wpf.Controls;
using Xunit;
using NotificationControl = Notification.Wpf.Controls.Notification;

namespace Notification.Wpf.Tests
{
    /// <summary>
    /// Regression coverage for the overlay lifecycle fixes (#2 and #3):
    /// #2 — the main-window hook must attach even when Application.Current.MainWindow was not
    /// available at overlay creation (it used to be wired exactly once and silently skipped),
    /// and an overlay whose hook never attached must Close itself instead of Hide (a hidden,
    /// unhooked overlay blocks OnLastWindowClose).
    /// #3 — closing the main window must close all active notifications first (CloseCts
    /// cancellation, reported as Cancelled) before the overlay window goes away, so no
    /// progress polling / countdown work survives as an orphan.
    /// R1 — a window that merely carries the "ToastWindow" title but is NOT managed by
    /// NotificationManager has no main-window hook to close it later; it must be Closed
    /// (not Hidden) when its last notification closes, or it blocks OnLastWindowClose.
    /// </summary>
    // Shares static Areas/_window/hook state with the other UI tests — same collection keeps
    // them serialized; StaTask.ResetSharedWpfState resets the state before every test.
    [Collection("NotificationUiState")]
    public class OverlayLifecycleTests
    {
        [Fact]
        public void Overlay_created_without_main_window_gets_hooked_on_next_show_and_main_close_cleans_up()
        {
            StaTask.Run(async () =>
            {
                // ResetSharedWpfState: Application exists, MainWindow == null, hook cleared,
                // _window cleared — the first Show below therefore cannot attach the hook.
                var mgr = new NotificationManager();

                mgr.Show(new NotificationRequest
                {
                    Message = "seed",
                    ExpirationTime = TimeSpan.FromSeconds(30)
                });

                Window overlay = GetOverlayWindow();
                Assert.NotNull(overlay);
                Assert.False(GetIsOverlayHooked(),
                    "With no main window the hook must not attach at overlay creation.");

                // The overlay's own area must be loaded before notifications can land in it.
                NotificationArea overlayArea = FindChildByName<NotificationArea>(overlay, null);
                Assert.NotNull(overlayArea);
                DateTime loadDeadline = DateTime.UtcNow.AddSeconds(5);
                while (!overlayArea.IsLoaded && DateTime.UtcNow < loadDeadline)
                    await Task.Delay(50);
                Assert.True(overlayArea.IsLoaded, "Overlay NotificationArea must load.");

                Task<NotificationCloseResult> task = mgr.ShowAsync(new NotificationRequest
                {
                    Message = "live",
                    ExpirationTime = TimeSpan.MaxValue,
                    CloseOnClick = false,
                    ShowCloseButton = true
                });

                Button closeBtn = null;
                DateTime btnDeadline = DateTime.UtcNow.AddSeconds(5);
                while (closeBtn == null && DateTime.UtcNow < btnDeadline)
                {
                    closeBtn = FindChildByName<Button>(overlay, "PART_CloseButton");
                    if (closeBtn == null)
                        await Task.Delay(50);
                }
                Assert.True(closeBtn != null, "The live notification must render inside the overlay.");

                // Stage a real main window AFTER the overlay exists — the case the old one-shot
                // wiring never recovered from.
                var mainWindow = new Window { Width = 200, Height = 120, ShowInTaskbar = false };
                try
                {
                    mainWindow.Show();
                    Application.Current.MainWindow = mainWindow;

                    // Next Show must (re)attach the hook now that a main window exists.
                    mgr.Show(new NotificationRequest
                    {
                        Message = "retrigger",
                        ExpirationTime = TimeSpan.FromSeconds(30)
                    });
                    Assert.True(GetIsOverlayHooked(),
                        "EnsureOverlayMainHook must attach on the next Show once MainWindow exists.");

                    // Closing the main window must close notifications first (Cancelled), then
                    // the overlay itself, and clear the hook.
                    mainWindow.Close();

                    NotificationCloseResult result =
                        await task.WaitAsync(TimeSpan.FromSeconds(5));
                    Assert.Equal(NotificationCloseReason.Cancelled, result.Reason);

                    Assert.False(GetIsOverlayHooked(),
                        "The handler must clear the hook state when it fires.");

                    DateTime closeDeadline = DateTime.UtcNow.AddSeconds(5);
                    while (overlay.IsLoaded && DateTime.UtcNow < closeDeadline)
                        await Task.Delay(50);
                    Assert.False(overlay.IsLoaded,
                        "The overlay must be Closed together with the main window, not left hidden.");
                    Assert.True(GetOverlayWindow() == null,
                        "A closed overlay must drop the static _window reference.");
                }
                finally
                {
                    if (mainWindow.IsLoaded)
                        mainWindow.Close();
                }
            }, TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void Unhooked_overlay_is_closed_not_hidden_when_last_notification_closes()
        {
            StaTask.Run(async () =>
            {
                // MainWindow stays null for this test → EnsureOverlayMainHook always skips →
                // the overlay runs unhooked, which is exactly the self-heal scenario.
                var mgr = new NotificationManager();

                mgr.Show(new NotificationRequest
                {
                    Message = "seed",
                    ExpirationTime = TimeSpan.FromSeconds(30)
                });

                Window overlay = GetOverlayWindow();
                Assert.NotNull(overlay);
                Assert.False(GetIsOverlayHooked());

                NotificationArea overlayArea = FindChildByName<NotificationArea>(overlay, null);
                Assert.NotNull(overlayArea);
                DateTime loadDeadline = DateTime.UtcNow.AddSeconds(5);
                while (!overlayArea.IsLoaded && DateTime.UtcNow < loadDeadline)
                    await Task.Delay(50);
                Assert.True(overlayArea.IsLoaded, "Overlay NotificationArea must load.");

                var request = new NotificationRequest
                {
                    Message = "live",
                    ExpirationTime = TimeSpan.MaxValue,
                    // Must stay true: the X button's close is wired through the CloseOnClick
                    // attached property (CloseOnClickChanged subscribes OnCloseButtonOnClick);
                    // false would make the close below a silent no-op and hang this task.
                    CloseOnClick = true,
                    ShowCloseButton = true
                };
                Task<NotificationCloseResult> task = mgr.ShowAsync(request);

                Button closeBtn = null;
                DateTime btnDeadline = DateTime.UtcNow.AddSeconds(5);
                while (closeBtn == null && DateTime.UtcNow < btnDeadline)
                {
                    closeBtn = FindChildByName<Button>(overlay, "PART_CloseButton");
                    if (closeBtn == null)
                        await Task.Delay(50);
                }
                Assert.True(closeBtn != null, "The live notification must render inside the overlay.");

                // Close ALL notifications in the overlay: the seed notification may have landed
                // in the tree too (Window.Show can fire Loaded synchronously), and the tail logic
                // under test runs when the LAST one closes (active count reaches zero).
                var notes = FindAllByType<NotificationControl>(overlay);
                Assert.True(notes.Count >= 1, "The notification control must exist in the overlay.");
                foreach (NotificationControl n in notes)
                    n.Close();

                await task.WaitAsync(TimeSpan.FromSeconds(5));

                DateTime closeDeadline = DateTime.UtcNow.AddSeconds(5);
                while (overlay.IsLoaded && DateTime.UtcNow < closeDeadline)
                    await Task.Delay(50);
                Assert.False(overlay.IsLoaded,
                    "An unhooked overlay must be Closed (IsLoaded false), not Hidden (IsLoaded true).");
                Assert.True(GetOverlayWindow() == null,
                    "Closing the overlay must drop the static _window so the next Show recreates it.");
            }, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// R1: a window that merely carries the "ToastWindow" title but is NOT the
        /// NotificationManager-owned overlay has no hook to close it later — Hide would leave
        /// it loaded-but-invisible, blocking OnLastWindowClose. The Close() tail must Close it
        /// (the pre-Hide baseline behavior) instead of Hide.
        /// </summary>
        [Fact]
        public void Unmanaged_toast_window_is_closed_not_hidden_when_last_notification_closes()
        {
            StaTask.Run(async () =>
            {
                // A named area keeps ShowContent from creating the managed overlay, so
                // _window stays null and IsManagedOverlay(window) is false — the window below
                // is found only by its "ToastWindow" title, exactly the unmanaged path under test.
                var area = new NotificationArea { Name = "UnmanagedToastArea" };
                var window = new Window
                {
                    Title = "ToastWindow",
                    Width = 400,
                    Height = 300,
                    Content = area,
                    ShowInTaskbar = false
                };

                bool windowClosed = false;
                window.Closed += (_, _) => windowClosed = true;

                try
                {
                    window.Show();

                    DateTime loadDeadline = DateTime.UtcNow.AddSeconds(5);
                    while (!area.IsLoaded && DateTime.UtcNow < loadDeadline)
                        await Task.Delay(50);
                    Assert.True(area.IsLoaded, "NotificationArea must be loaded (registered in Areas) before Show.");

                    Assert.True(GetOverlayWindow() == null,
                        "The managed overlay must not exist, otherwise Close() would resolve it by title instead of the unmanaged window.");

                    var mgr = new NotificationManager();
                    Task<NotificationCloseResult> task = mgr.ShowAsync(new NotificationRequest
                    {
                        AreaName = "UnmanagedToastArea",
                        Message = "live",
                        ExpirationTime = TimeSpan.MaxValue,
                        CloseOnClick = false,
                        ShowCloseButton = true
                    });

                    NotificationControl notification = null;
                    DateTime noteDeadline = DateTime.UtcNow.AddSeconds(5);
                    while (notification == null && DateTime.UtcNow < noteDeadline)
                    {
                        notification = FindAllByType<NotificationControl>(window).FirstOrDefault();
                        if (notification == null)
                            await Task.Delay(50);
                    }
                    Assert.True(notification != null, "The notification must render inside the unmanaged window.");

                    notification.Close();
                    await task.WaitAsync(TimeSpan.FromSeconds(5));

                    // The Close() tail must Close this unmanaged window: Hide would keep
                    // IsLoaded true and never raise Closed. Closed fires before the Unloaded
                    // pass flips IsLoaded, so wait for BOTH signals before asserting.
                    DateTime closeDeadline = DateTime.UtcNow.AddSeconds(5);
                    while ((!windowClosed || window.IsLoaded) && DateTime.UtcNow < closeDeadline)
                        await Task.Delay(50);

                    Assert.True(windowClosed,
                        "An unmanaged ToastWindow host must be Closed (Closed event raised), not Hidden — " +
                        "a hidden unhooked window blocks OnLastWindowClose.");
                    Assert.False(window.IsLoaded,
                        "IsLoaded must be false after Close; a Hidden window keeps IsLoaded true.");
                }
                finally
                {
                    if (window.IsLoaded)
                        window.Close();
                    // Unloaded (→ NotificationManager.RemoveArea) is dispatched asynchronously;
                    // let it flush so the static Areas list is clean for the next test.
                    await Task.Delay(200);
                }
            }, TimeSpan.FromSeconds(30));
        }

        private static Window GetOverlayWindow() =>
            (Window)typeof(NotificationManager)
                .GetField("_window", BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null);

        private static bool GetIsOverlayHooked() =>
            (bool)typeof(NotificationManager)
                .GetProperty("IsOverlayHooked", BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null);

        private static T FindChildByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root == null) return null;
            if (root is T typed && (name == null || typed.Name == name)) return typed;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                T found = FindChildByName<T>(VisualTreeHelper.GetChild(root, i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static System.Collections.Generic.List<T> FindAllByType<T>(DependencyObject root)
            where T : DependencyObject
        {
            var result = new System.Collections.Generic.List<T>();
            CollectByType(root, result);
            return result;
        }

        private static void CollectByType<T>(DependencyObject root, System.Collections.Generic.List<T> result)
            where T : DependencyObject
        {
            if (root == null) return;
            if (root is T typed) result.Add(typed);
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
                CollectByType(VisualTreeHelper.GetChild(root, i), result);
        }
    }
}
