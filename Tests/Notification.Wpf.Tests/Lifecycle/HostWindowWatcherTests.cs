using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

using Notification.Wpf.Base;

using Xunit;

namespace Notification.Wpf.Tests.Lifecycle
{
    [Collection("Gui")]
    [Trait("Category", "Gui")]
    public class HostWindowWatcherTests
    {
        /// <summary>
        /// Stands in for <c>Application.Windows</c>: a window leaves the list from its own Closed
        /// handler, which is subscribed before the watcher's, as WPF removes a window before raising Closed.
        /// </summary>
        private sealed class WindowList
        {
            private readonly List<Window> _windows = new List<Window>();

            public IReadOnlyList<Window> Snapshot() => _windows.ToArray();

            [MethodImpl(MethodImplOptions.NoInlining)]
            public Window Open()
            {
                Window window = new Window
                {
                    Left = -10000,
                    Top = 0,
                    Width = 100,
                    Height = 100,
                    ShowActivated = false,
                    ShowInTaskbar = false
                };
                _windows.Add(window);
                window.Closed += (_, _) => _windows.Remove(window);
                window.Show();
                return window;
            }
        }

        private static HostWindowWatcher CreateWatcher(WindowList list, Action onLast, bool lastWindowMode = true)
        {
            return new HostWindowWatcher(list.Snapshot, () => lastWindowMode, onLast);
        }

        [Fact]
        public void LastTrackedWindowClosed_InvokesCallbackOnce()
        {
            GuiThread.Run(async () =>
            {
                WindowList list = new WindowList();
                int calls = 0;
                HostWindowWatcher watcher = CreateWatcher(list, () => calls++);
                Window window = list.Open();
                watcher.Scan();

                window.Close();
                await Task.Delay(50);

                Assert.Equal(1, calls);
                Assert.Equal(0, watcher.TrackedCount);
            });
        }

        [Fact]
        public void TrackedWindowClosed_OtherWindowOpen_DoesNotInvoke()
        {
            GuiThread.Run(async () =>
            {
                WindowList list = new WindowList();
                int calls = 0;
                HostWindowWatcher watcher = CreateWatcher(list, () => calls++);
                Window first = list.Open();
                Window second = list.Open();
                try
                {
                    watcher.Scan();

                    first.Close();
                    await Task.Delay(50);

                    Assert.Equal(0, calls);
                    Assert.Equal(1, watcher.TrackedCount);
                }
                finally
                {
                    second.Close();
                }
            });
        }

        [Fact]
        public void WindowOpenedAfterScan_IsTrackedAfterAnotherCloses()
        {
            GuiThread.Run(async () =>
            {
                WindowList list = new WindowList();
                int calls = 0;
                HostWindowWatcher watcher = CreateWatcher(list, () => calls++);
                Window first = list.Open();
                watcher.Scan();

                // Opened after the scan, so nothing tracks it yet.
                Window late = list.Open();
                try
                {
                    first.Close();
                    await Task.Delay(50);

                    // The rescan on first's close picked the late window up, so closing it is the last close.
                    Assert.Equal(0, calls);
                    Assert.Equal(1, watcher.TrackedCount);

                    late.Close();
                    await Task.Delay(50);

                    Assert.Equal(1, calls);
                }
                finally
                {
                    late.Close();
                }
            });
        }

        [Fact]
        public void NotLastWindowMode_DoesNotInvoke()
        {
            GuiThread.Run(async () =>
            {
                WindowList list = new WindowList();
                int calls = 0;
                HostWindowWatcher watcher = CreateWatcher(list, () => calls++, lastWindowMode: false);
                Window window = list.Open();
                watcher.Scan();

                window.Close();
                await Task.Delay(50);

                Assert.Equal(0, calls);
                Assert.Equal(0, watcher.TrackedCount);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<WeakReference> OpenTrackAndCloseAsync(HostWindowWatcher watcher, WindowList list)
        {
            Window window = list.Open();
            watcher.Scan();
            Assert.Equal(1, watcher.TrackedCount);
            WeakReference reference = new WeakReference(window);

            window.Close();
            await Task.Delay(50);
            return reference;
        }

        [Fact]
        public void Closed_ReleasesWindowFromTracking()
        {
            GuiThread.Run(async () =>
            {
                WindowList list = new WindowList();
                HostWindowWatcher watcher = CreateWatcher(list, () => { });

                WeakReference reference = await OpenTrackAndCloseAsync(watcher, list);

                Assert.Equal(0, watcher.TrackedCount);
                await GcAssert.CollectedAsync(reference);
            });
        }
    }
}
