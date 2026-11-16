using System;
using System.Threading.Tasks;
using System.Windows;

using Notification.Wpf.Base;
using Notification.Wpf.Controls;

namespace Notification.Wpf.Tests.Lifecycle
{
    /// <summary>
    /// Hosts a <see cref="NotificationArea"/> in an off-screen window so lifecycle tests
    /// can exercise real WPF loading without stealing focus from the desktop.
    /// Must be created and disposed on an STA thread that runs a dispatcher.
    /// </summary>
    internal sealed class GuiHost : IDisposable
    {
        private GuiHost(Window window, NotificationArea area, NotificationManager manager)
        {
            Window = window;
            Area = area;
            Manager = manager;
        }

        public Window Window { get; }

        public NotificationArea Area { get; }

        public NotificationManager Manager { get; }

        public static async Task<GuiHost> CreateAsync(string areaName = "gui")
        {
            NotificationArea area = new NotificationArea { Name = areaName };
            Window window = new Window
            {
                Left = -10000,
                Top = 0,
                Width = 400,
                Height = 300,
                ShowActivated = false,
                ShowInTaskbar = false,
                Content = area
            };

            TaskCompletionSource<bool> loaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            area.Loaded += (_, _) => loaded.TrySetResult(true);

            window.Show();
            if (!area.IsLoaded)
                await loaded.Task;

            NotificationManager manager = new NotificationManager(window.Dispatcher);
            return new GuiHost(window, area, manager);
        }

        public void Dispose()
        {
            // Reset in finally: a throwing Close would otherwise leave the overlay and areas registered and
            // cascade into every later GUI test.
            try
            {
                Window.Close();
            }
            finally
            {
                NotificationManager.ResetStateForTests();
            }
        }
    }
}
