using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;

namespace Notification.Wpf
{
    /// <summary>
    /// Interaction logic for ToastWindow
    /// </summary>
    public partial class NotificationsOverlayWindow : Window
    {
        #region MaxWindowItems : int - Maximum Window Items

        /// <summary>Maximum Window Items</summary>
        public static readonly DependencyProperty MaxWindowItemsProperty =
            DependencyProperty.Register(
                nameof(MaxWindowItems),
                typeof(uint),
                typeof(NotificationsOverlayWindow),
                new PropertyMetadata(NotificationConstants.NotificationsOverlayWindowMaxCount));

        /// <summary>Maximum Window Items</summary>
        public uint MaxWindowItems { get => (uint)GetValue(MaxWindowItemsProperty); set => SetValue(MaxWindowItemsProperty, value); }

        #endregion

        #region MessagePosition : NotificationPosition - Позиция сообщений в окне

        /// <summary>Позиция сообщений в окне</summary>
        public static readonly DependencyProperty MessagePositionProperty =
            DependencyProperty.Register(
                nameof(MessagePosition),
                typeof(NotificationPosition),
                typeof(NotificationsOverlayWindow),
                new PropertyMetadata(NotificationConstants.MessagePosition));

        /// <summary>Позиция сообщений в окне</summary>
        public NotificationPosition MessagePosition { get => (NotificationPosition)GetValue(MessagePositionProperty); set => SetValue(MessagePositionProperty, value); }

        #endregion

        #region CollapseProgressAutoIfMoreMessages : bool - Need collapse notification if count more that maximum

        /// <summary>Need collapse notification if count more that maximum</summary>
        public static readonly DependencyProperty CollapseProgressAutoIfMoreMessagesProperty =
            DependencyProperty.Register(
                nameof(CollapseProgressAutoIfMoreMessages),
                typeof(bool),
                typeof(NotificationsOverlayWindow),
                new PropertyMetadata(NotificationConstants.CollapseProgressIfMoreRows));

        /// <summary>Need collapse notification if count more that maximum</summary>
        public bool CollapseProgressAutoIfMoreMessages { get => (bool)GetValue(CollapseProgressAutoIfMoreMessagesProperty); set => SetValue(CollapseProgressAutoIfMoreMessagesProperty, value); }

        #endregion

        #region Window styles : hide alt+tab toastwindow

        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        #endregion

        public NotificationsOverlayWindow()
        {
            InitializeComponent();
            this.Loaded += NotificationsOverlayWindow_Loaded;
        }

        private void NotificationsOverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;

            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }
    }
}
