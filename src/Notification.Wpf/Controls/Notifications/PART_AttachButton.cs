using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Notifications.Wpf.View;

namespace Notification.Wpf.Controls
{
    [TemplatePart(Name = "PART_AttachButton", Type = typeof(Button))]
    public partial class Notification : ContentControl
    {

        public static readonly RoutedEvent NotificationAttachInvokedEvent = EventManager.RegisterRoutedEvent(
            "NotificationAttachInvoked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));

        public static readonly RoutedEvent NotificationAttachEvent = EventManager.RegisterRoutedEvent(
            "NotificationAttach", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));

        public event RoutedEventHandler NotificationAttachInvoked
        {
            add => AddHandler(NotificationAttachInvokedEvent, value);
            remove => RemoveHandler(NotificationAttachInvokedEvent, value);
        }

        public event RoutedEventHandler NotificationAttach
        {
            add => AddHandler(NotificationAttachEvent, value);
            remove => RemoveHandler(NotificationAttachEvent, value);
        }

        public static bool GetAttachOnClick(DependencyObject obj)
        {
            return (bool)obj.GetValue(AttachOnClickProperty);
        }

        public static void SetAttachOnClick(DependencyObject obj, bool value)
        {
            obj.SetValue(AttachOnClickProperty, value);
        }

        public static readonly DependencyProperty AttachOnClickProperty =
            DependencyProperty.RegisterAttached("AttachOnClick", typeof(NotificationContent), typeof(Notification),
                new FrameworkPropertyMetadata(new NotificationContent
                {
                    Message = string.Empty,
                    Title = string.Empty,
                    TrimType = NotificationTextTrimType.NoTrim,
                    Type = NotificationType.Notification
                }, AttachOnClickChanged));

        private static void AttachOnClickChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is not Button button)
            {
                return;
            }

            var value = (NotificationContent)dependencyPropertyChangedEventArgs.NewValue;

            if (value is not null)
            {
                button.Click += (sender, args) =>
                {
                    var window = new Window();
                    var win_content = new TextContentView { DataContext = value };
                    window.Content = win_content;
                    window.Title = "Message";
                    window.Height = 500;
                    window.Width = 650;
                    window.WindowStyle = WindowStyle.None;
                    window.MouseDown += (Sender, Args) =>
                    {
                        if (Args.ChangedButton != MouseButton.Left) return;
                        window.DragMove();
                    };
                    window.MouseDoubleClick += (Sender, Args) =>
                    {
                        if (window.WindowState == WindowState.Maximized)
                        {
                            window.WindowState = WindowState.Normal;
                        }
                        else if (window.WindowState == WindowState.Normal)
                        {
                            window.WindowState = WindowState.Maximized;
                        }
                    };
                    window.Show();
                };
            }
        }


        private void OnAttachButtonOnClick(object sender, RoutedEventArgs args)
        {
            if (sender is not Button button) return;

            button.Click -= OnAttachButtonOnClick;
            Attach();
        }

        public void Attach()
        {
            if (IsClosing)
            {
                return;
            }

            RaiseEvent(new RoutedEventArgs(NotificationCloseInvokedEvent));

        }


    }
}
