using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms
{
    internal enum TaskbarPosition
    {
        Unknown = -1,
        Left,
        Top,
        Right,
        Bottom,
    }

    internal static class Taskbar
    {
        private enum ABS
        {
            AutoHide = 0x01,
            AlwaysOnTop = 0x02,
        }

        ////private enum ABE : uint
        private enum AppBarEdge : uint
        {
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3
        }

        ////private enum ABM : uint
        private enum AppBarMessage : uint
        {
            New = 0x00000000,
            Remove = 0x00000001,
            QueryPos = 0x00000002,
            SetPos = 0x00000003,
            GetState = 0x00000004,
            GetTaskbarPos = 0x00000005,
            Activate = 0x00000006,
            GetAutoHideBar = 0x00000007,
            SetAutoHideBar = 0x00000008,
            WindowPosChanged = 0x00000009,
            SetState = 0x0000000A,
        }

        private const string ClassName = "Shell_TrayWnd";
        private static APPBARDATA _appBarData;

        /// <summary>Static initializer of the <see cref="Taskbar" /> class.</summary>
        static Taskbar()
        {
            _appBarData = new APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = FindWindow(Taskbar.ClassName, null)
            };
        }

        /// <summary>
        ///   Gets a value indicating whether the taskbar is always on top of other windows.
        /// </summary>
        /// <value><c>true</c> if the taskbar is always on top of other windows; otherwise, <c>false</c>.</value>
        /// <remarks>This property always returns <c>false</c> on Windows 7 and newer.</remarks>
        public static bool AlwaysOnTop
        {
            get
            {
                int state = SHAppBarMessage(AppBarMessage.GetState, ref _appBarData).ToInt32();
                return ((ABS)state).HasFlag(ABS.AlwaysOnTop);
            }
        }

        /// <summary>
        ///   Gets a value indicating whether the taskbar is automatically hidden when inactive.
        /// </summary>
        /// <value><c>true</c> if the taskbar is set to auto-hide is enabled; otherwise, <c>false</c>.</value>
        public static bool AutoHide
        {
            get
            {
                int state = SHAppBarMessage(AppBarMessage.GetState, ref _appBarData).ToInt32();
                return ((ABS)state).HasFlag(ABS.AutoHide);
            }
        }

        /// <summary>Gets the current display bounds of the taskbar.</summary>
        public static Rectangle CurrentBounds
        {
            get
            {
                var rect = new RECT();
                if (GetWindowRect(Handle, ref rect))
                    return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);

                return Rectangle.Empty;
            }
        }

        /// <summary>Gets the display bounds when the taskbar is fully visible.</summary>
        public static Rectangle DisplayBounds
        {
            get
            {
                if (RefreshBoundsAndPosition())
                    return Rectangle.FromLTRB(_appBarData.rect.Left,
                                              _appBarData.rect.Top,
                                              _appBarData.rect.Right,
                                              _appBarData.rect.Bottom);

                return CurrentBounds;
            }
        }

        /// <summary>Gets the taskbar's window handle.</summary>
        public static IntPtr Handle
        {
            get { return _appBarData.hWnd; }
        }

        /// <summary>Gets the taskbar's position on the screen.</summary>
        public static TaskbarPosition Position
        {
            get
            {
                if (RefreshBoundsAndPosition())
                    return (TaskbarPosition)_appBarData.uEdge;

                return TaskbarPosition.Unknown;
            }
        }

        /// <summary>Hides the taskbar.</summary>
        public static void Hide()
        {
            const int SW_HIDE = 0;
            ShowWindow(Handle, SW_HIDE);
        }

        /// <summary>Shows the taskbar.</summary>
        public static void Show()
        {
            const int SW_SHOW = 1;
            ShowWindow(Handle, SW_SHOW);
        }

        private static bool RefreshBoundsAndPosition()
        {
            //! SHAppBarMessage returns IntPtr.Zero **if it fails**
            return SHAppBarMessage(AppBarMessage.GetTaskbarPos, ref _appBarData) != IntPtr.Zero;
        }

        #region DllImports

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr SHAppBarMessage(AppBarMessage dwMessage, [In] ref APPBARDATA pData);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hwnd, int command);

        #endregion DllImports

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public AppBarEdge uEdge;
            public RECT rect;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}