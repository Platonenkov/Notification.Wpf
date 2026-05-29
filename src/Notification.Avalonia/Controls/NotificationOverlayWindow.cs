using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Notification.Core;

namespace Notification.Avalonia.Controls
{
    /// <summary>
    /// Separate transparent topmost window for displaying notifications outside the main window.
    /// Positioned in the corner/edge of the screen based on NotificationPosition.
    /// Does not cover the full screen — only the notification area.
    /// Hidden from Alt+Tab via WS_EX_TOOLWINDOW on Windows.
    /// </summary>
    internal sealed class NotificationOverlayWindow : Window
    {
        private readonly AvaloniaNotificationHost _host;

        private const double WindowWidth = 400;
        private const double WindowHeight = 600;

        public AvaloniaNotificationHost Host => _host;

        public NotificationOverlayWindow()
        {
            // Transparent borderless window
            WindowDecorations = WindowDecorations.None;
            ShowInTaskbar = false;
            Topmost = true;
            Background = Brushes.Transparent;
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
            CanResize = false;
            ShowActivated = false;

            Width = WindowWidth;
            Height = WindowHeight;

            // Create notification host targeting this window
            _host = new AvaloniaNotificationHost(this);
        }

        /// <summary>
        /// Bind overlay lifetime to parent window — close overlay when parent closes.
        /// Also hides from Alt+Tab on Windows.
        /// </summary>
        public void BindToParent(Window parentWindow)
        {
            if (parentWindow == null)
                return;

            parentWindow.Closing += (s, e) =>
            {
                try
                {
                    Close();
                }
                catch
                {
                    // Ignored — parent may already be disposing
                }
            };
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            ApplyToolWindowStyle();
        }

        /// <summary>
        /// On Windows, apply WS_EX_TOOLWINDOW extended style to hide from Alt+Tab.
        /// </summary>
        private void ApplyToolWindowStyle()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                IntPtr handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                    return;

                const int GWL_EXSTYLE = -20;
                const int WS_EX_TOOLWINDOW = 0x00000080;

                int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                SetWindowLong(handle, GWL_EXSTYLE, exStyle);
            }
            catch
            {
                // Non-critical — overlay just stays visible in Alt+Tab
            }
        }

        /// <summary>
        /// Position the overlay window on the correct edge of the screen
        /// based on the current notification position.
        /// </summary>
        public void ApplyPositionOnScreen(Window parentWindow, NotificationPosition position)
        {
            var screens = parentWindow?.Screens ?? Screens;

            var targetScreen = parentWindow is not null
                    ? screens?.ScreenFromWindow(parentWindow) ?? screens?.Primary
                    : screens?.Primary;

            if (targetScreen is null)
                return;

            PixelRect workArea = targetScreen.WorkingArea;
            double scaling = targetScreen.Scaling;

            // WorkArea is in physical pixels, convert to DIPs for positioning
            double areaX = workArea.X / scaling;
            double areaY = workArea.Y / scaling;
            double areaW = workArea.Width / scaling;
            double areaH = workArea.Height / scaling;

            double x;
            double y;

            // Horizontal
            switch (position)
            {
                case NotificationPosition.TopLeft:
                case NotificationPosition.BottomLeft:
                case NotificationPosition.CenterLeft:
                    x = areaX;
                    break;
                case NotificationPosition.TopCenter:
                case NotificationPosition.BottomCenter:
                case NotificationPosition.Center:
                    x = areaX + (areaW - WindowWidth) / 2;
                    break;
                default: // Right
                    x = areaX + areaW - WindowWidth;
                    break;
            }

            // Vertical
            switch (position)
            {
                case NotificationPosition.TopLeft:
                case NotificationPosition.TopCenter:
                case NotificationPosition.TopRight:
                    y = areaY;
                    break;
                case NotificationPosition.CenterLeft:
                case NotificationPosition.Center:
                case NotificationPosition.CenterRight:
                    y = areaY + (areaH - WindowHeight) / 2;
                    break;
                default: // Bottom
                    y = areaY + areaH - WindowHeight;
                    break;
            }

            // Position uses physical pixels
            Position = new PixelPoint((int)(x * scaling), (int)(y * scaling));
        }

        #region Win32 Interop

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion
    }
}
