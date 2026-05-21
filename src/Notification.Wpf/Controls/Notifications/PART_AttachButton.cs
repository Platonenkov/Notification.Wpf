using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Notifications.Wpf.View;

namespace Notification.Wpf.Controls
{
    [TemplatePart(Name = "PART_AttachButton", Type = typeof(Button))]
    public partial class Notification : ContentControl
    {

        /// <summary>Identifies the routed event raised when the notification attach sequence is invoked.</summary>
        public static readonly RoutedEvent NotificationAttachInvokedEvent = EventManager.RegisterRoutedEvent(
            "NotificationAttachInvoked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));

        /// <summary>Identifies the routed event raised when the notification is attached.</summary>
        public static readonly RoutedEvent NotificationAttachEvent = EventManager.RegisterRoutedEvent(
            "NotificationAttach", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Notification));

        /// <summary>Occurs when the notification attach sequence is invoked.</summary>
        public event RoutedEventHandler NotificationAttachInvoked
        {
            add => AddHandler(NotificationAttachInvokedEvent, value);
            remove => RemoveHandler(NotificationAttachInvokedEvent, value);
        }

        /// <summary>Occurs when the notification is attached.</summary>
        public event RoutedEventHandler NotificationAttach
        {
            add => AddHandler(NotificationAttachEvent, value);
            remove => RemoveHandler(NotificationAttachEvent, value);
        }

        /// <summary>Gets the value of the <see cref="AttachOnClickProperty"/> attached property for the specified object.</summary>
        /// <param name="obj">The object from which to read the attached property value.</param>
        /// <returns><see langword="true"/> if clicking the element opens the attached content; otherwise, <see langword="false"/>.</returns>
        public static bool GetAttachOnClick(DependencyObject obj)
        {
            return (bool)obj.GetValue(AttachOnClickProperty);
        }

        /// <summary>Sets the value of the <see cref="AttachOnClickProperty"/> attached property for the specified object.</summary>
        /// <param name="obj">The object on which to set the attached property value.</param>
        /// <param name="value"><see langword="true"/> to open the attached content when the element is clicked; otherwise, <see langword="false"/>.</param>
        public static void SetAttachOnClick(DependencyObject obj, bool value)
        {
            obj.SetValue(AttachOnClickProperty, value);
        }

        /// <summary>Identifies the AttachOnClick attached property, which opens a window displaying the associated <see cref="NotificationContent"/> when the target button is clicked.</summary>
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

        /// <summary>Attaches the notification by raising the close-invoked routed event, unless the notification is already closing.</summary>
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
