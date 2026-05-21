using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Notification.Wpf.Utils;
using Notification.Wpf.View;
using Notifications.Wpf.View;

namespace Notification.Wpf.Controls
{
    [TemplatePart(Name = "PART_CloseButton", Type = typeof(Button))]
    public partial class Notification : ContentControl
    {
        private TimeSpan _closingAnimationTime = TimeSpan.Zero;

        /// <summary>Gets or sets a value indicating whether the notification is currently in the process of closing.</summary>
        public bool IsClosing { get; set; }

        /// <summary>Identifies the routed event raised when the notification close sequence is invoked, before the closing animation finishes.</summary>
        public static readonly RoutedEvent NotificationCloseInvokedEvent = EventManager.RegisterRoutedEvent(
            "NotificationCloseInvoked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));

        /// <summary>Identifies the routed event raised after the notification has been closed and the closing animation has completed.</summary>
        public static readonly RoutedEvent NotificationClosedEvent = EventManager.RegisterRoutedEvent(
            "NotificationClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));


        /// <summary>Occurs when the notification close sequence is invoked, before the closing animation finishes.</summary>
        public event RoutedEventHandler NotificationCloseInvoked
        {
            add => AddHandler(NotificationCloseInvokedEvent, value);
            remove => RemoveHandler(NotificationCloseInvokedEvent, value);
        }

        /// <summary>Occurs after the notification has been closed and the closing animation has completed.</summary>
        public event RoutedEventHandler NotificationClosed
        {
            add => AddHandler(NotificationClosedEvent, value);
            remove => RemoveHandler(NotificationClosedEvent, value);
        }

        /// <summary>Gets the value of the <see cref="CloseOnClickProperty"/> attached property for the specified object.</summary>
        /// <param name="obj">The object from which to read the attached property value.</param>
        /// <returns><see langword="true"/> if clicking the element closes the parent notification; otherwise, <see langword="false"/>.</returns>
        public static bool GetCloseOnClick(DependencyObject obj)
        {
            return (bool)obj.GetValue(CloseOnClickProperty);
        }

        /// <summary>Sets the value of the <see cref="CloseOnClickProperty"/> attached property for the specified object.</summary>
        /// <param name="obj">The object on which to set the attached property value.</param>
        /// <param name="value"><see langword="true"/> to close the parent notification when the element is clicked; otherwise, <see langword="false"/>.</param>
        public static void SetCloseOnClick(DependencyObject obj, bool value)
        {
            obj.SetValue(CloseOnClickProperty, value);
        }

        /// <summary>Identifies the CloseOnClick attached property, which closes the parent notification when the target button is clicked.</summary>
        public static readonly DependencyProperty CloseOnClickProperty =
            DependencyProperty.RegisterAttached("CloseOnClick", typeof(bool), typeof(Notification), new FrameworkPropertyMetadata(false, CloseOnClickChanged));

        private static void CloseOnClickChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is not Button button)
            {
                return;
            }

            var value = (bool)dependencyPropertyChangedEventArgs.NewValue;

            if (value)
            {
                button.Click += (sender, args) =>
                {
                    var notification = VisualTreeHelperExtensions.GetParent<Notification>(button);
                    notification?.Close();
                };
            }
        }

        private void OnCloseButtonOnClick(object sender, RoutedEventArgs args)
        {
            if (sender is not Button button) return;

            button.Click -= OnCloseButtonOnClick;

            Close();
        }

        //TODO: .NET40
        /// <summary>Closes the notification, raising the close events and closing the host toast window when no notifications remain.</summary>
        /// <param name="overlayWindow">The optional toast overlay window that hosts the notification. When omitted, the host window is resolved automatically.</param>
        public async void Close(Window overlayWindow = null)
        {
            if (IsClosing)
            {
                return;
            }

            IsClosing = true;

            RaiseEvent(new RoutedEventArgs(NotificationCloseInvokedEvent));
            await Task.Delay(_closingAnimationTime);
            RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));

            const string toastWindowTitle = "ToastWindow";

            Window currentWindow = null;
            if (overlayWindow != null && overlayWindow.Title == toastWindowTitle)
                currentWindow = overlayWindow;

            if (currentWindow == null && Application.Current != null)
            {
                try
                {
                    currentWindow = Application.Current.Windows.OfType<Window>()
                        .FirstOrDefault(x => x.Title.Equals(toastWindowTitle));
                }
                catch (InvalidOperationException) { }
            }

            if (currentWindow == null) return;
            var notificationCount = VisualTreeHelperExtensions.GetActiveNotificationCount(currentWindow);

            if (notificationCount == 0)
                currentWindow.Close();
        }


    }
}
