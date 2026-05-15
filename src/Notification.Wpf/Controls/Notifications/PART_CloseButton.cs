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

        public bool IsClosing { get; set; }

        public static readonly RoutedEvent NotificationCloseInvokedEvent = EventManager.RegisterRoutedEvent(
            "NotificationCloseInvoked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));

        public static readonly RoutedEvent NotificationClosedEvent = EventManager.RegisterRoutedEvent(
            "NotificationClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));


        public event RoutedEventHandler NotificationCloseInvoked
        {
            add => AddHandler(NotificationCloseInvokedEvent, value);
            remove => RemoveHandler(NotificationCloseInvokedEvent, value);
        }

        public event RoutedEventHandler NotificationClosed
        {
            add => AddHandler(NotificationClosedEvent, value);
            remove => RemoveHandler(NotificationClosedEvent, value);
        }

        public static bool GetCloseOnClick(DependencyObject obj)
        {
            return (bool)obj.GetValue(CloseOnClickProperty);
        }

        public static void SetCloseOnClick(DependencyObject obj, bool value)
        {
            obj.SetValue(CloseOnClickProperty, value);
        }

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
        public async void Close()
        {
            if (IsClosing)
            {
                return;
            }

            IsClosing = true;

            RaiseEvent(new RoutedEventArgs(NotificationCloseInvokedEvent));
            await Task.Delay(_closingAnimationTime);
            RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));

            var currentWindow = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.Title.Equals("ToastWindow"));
            if (currentWindow == null) return;
            var notificationCount = VisualTreeHelperExtensions.GetActiveNotificationCount(currentWindow);

            if (notificationCount == 0)
                currentWindow.Close();

        }


    }
}
