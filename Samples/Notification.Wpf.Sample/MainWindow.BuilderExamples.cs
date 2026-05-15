using System;
using System.Windows;
using System.Windows.Media;
using Notification.Core;

namespace Notification.Wpf.Sample
{
    public partial class MainWindow
    {
        private void ShowBuilderApiExample(object sender, RoutedEventArgs e)
        {
            INotificationManager notifier = new NotificationManager();

            notifier.Show(NotificationBuilder
                .Create("Builder API", "This notification was created using the Builder pattern")
                .AsSuccess()
                .ExpiresInSeconds(5)
                .CloseOnClick()
                .Build());
        }

        private void ShowBuilderWarningExample(object sender, RoutedEventArgs e)
        {
            INotificationManager notifier = new NotificationManager();

            notifier.Show(NotificationBuilder
                .Create()
                .WithTitle("Warning")
                .WithMessage("Custom colors via Builder API")
                .AsWarning()
                .WithBackground("#FF8800")
                .ExpiresInSeconds(8)
                .Build());
        }

        private void ShowBuilderButtonsExample(object sender, RoutedEventArgs e)
        {
            INotificationManager notifier = new NotificationManager();

            notifier.Show(NotificationBuilder
                .Create("Confirm", "Do you want to proceed?")
                .OfType(NotificationType.Information)
                .NeverExpires()
                .WithLeftButton("Yes", () => MessageBox.Show("Confirmed!"))
                .WithRightButton("No", () => { })
                .Build());
        }

        private void ShowBuilderPriorityExample(object sender, RoutedEventArgs e)
        {
            INotificationManager notifier = new NotificationManager();

            notifier.Show(NotificationBuilder
                .Create("Critical Alert", "This is a high priority notification")
                .AsError()
                .WithPriority(NotificationPriority.Critical)
                .ExpiresInSeconds(10)
                .OnClick(() => MessageBox.Show("Clicked!"))
                .Build());
        }
    }
}
