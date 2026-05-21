using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        // Migration example: ShowProgressBar(ProgressBarOptions) replaces the obsolete
        // 16-parameter positional ShowProgressBar overload. See Migration.md, section 2.
        private async void ShowProgressBarOptionsExample(object sender, RoutedEventArgs e)
        {
            INotificationManager notifier = new NotificationManager();

            // Only the properties that differ from the defaults need to be set.
            using var progress = notifier.ShowProgressBar(new ProgressBarOptions
            {
                Title              = "Processing files",
                ShowCancelButton   = true,
                BaseWaitingMessage = "Calculation time",
                ProgressColor      = NotificationColor.LimeGreen,
            });

            for (int i = 0; i <= 100; i++)
            {
                progress.Cancel.ThrowIfCancellationRequested();
                // Tuple-free Report overload: value + message.
                progress.Report(i, $"File {i} of 100");
                await Task.Delay(20, progress.Cancel);
            }
        }

        // Migration example: NotificationBuilder.WithContent replaces the
        // Show(object content, ...) overload for arbitrary custom UI. See Migration.md, section 3.
        private void ShowBuilderCustomContentExample(object sender, RoutedEventArgs e)
        {
            INotificationManager notifier = new NotificationManager();

            StackPanel customContent = new StackPanel { Margin = new Thickness(12) };
            customContent.Children.Add(new TextBlock
            {
                Text = "Custom content via WithContent()",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
            });
            customContent.Children.Add(new ProgressBar
            {
                Height = 6,
                Margin = new Thickness(0, 8, 0, 0),
                IsIndeterminate = true,
            });

            notifier.Show(NotificationBuilder
                .Create()
                .WithContent(customContent)
                .ExpiresInSeconds(6)
                .Build());
        }
    }
}
