using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Notification.Core;

namespace Notification.Avalonia.Sample
{
    public partial class MainWindow : Window
    {
        private readonly INotificationService _service;
        private readonly INotificationEventService _events;
        private readonly AvaloniaNotificationManager _manager;

        public MainWindow()
        {
            InitializeComponent();

            ServiceCollection services = new ServiceCollection();
            services.AddAvaloniaNotifications(config =>
            {
                config.DefaultExpirationTime = TimeSpan.FromSeconds(5);
            });

            ServiceProvider provider = services.BuildServiceProvider();

            _manager = provider.GetRequiredService<AvaloniaNotificationManager>();
            _manager.SetHost(this);

            _service = provider.GetRequiredService<INotificationService>();
            _events = provider.GetRequiredService<INotificationEventService>();

            _events.NotificationLifecycleChanged += (sender, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    TextBlock log = this.FindControl<TextBlock>("TxtLog");
                    if (log != null)
                        log.Text = $"Event: {e.Stage} - {e.Title ?? "N/A"}";
                });
            };
        }

        #region Quick Actions

        private void OnShowSuccess(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder.Create("Success", "Operation completed").AsSuccess().Build());
        }

        private void OnShowWarning(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder.Create("Warning", "Disk space low").AsWarning().Build());
        }

        private void OnShowError(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder.Create("Error", "Connection failed").AsError().NeverExpires().Build());
        }

        private void OnShowInfo(object sender, RoutedEventArgs e)
        {
            _service.Show(NotificationBuilder.Create("Info", "Update available").AsInformation().Build());
        }

        private void OnDismissAll(object sender, RoutedEventArgs e)
        {
            _service.DismissAll();
        }

        private void OnOverlayModeChanged(object sender, RoutedEventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            if (chk == null || _manager == null)
                return;

            _manager.UseOverlayWindow = chk.IsChecked == true;
        }

        private void OnPositionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmbPosition = sender as ComboBox;
            if (cmbPosition == null || _manager == null)
                return;

            NotificationPosition position = cmbPosition.SelectedIndex switch
            {
                0 => NotificationPosition.TopLeft,
                1 => NotificationPosition.TopCenter,
                2 => NotificationPosition.TopRight,
                3 => NotificationPosition.BottomLeft,
                4 => NotificationPosition.BottomRight,
                5 => NotificationPosition.BottomCenter,
                6 => NotificationPosition.CenterLeft,
                7 => NotificationPosition.CenterRight,
                8 => NotificationPosition.Center,
                _ => NotificationPosition.BottomRight
            };

            _manager.Position = position;
        }

        private void OnShowImageTop(object sender, RoutedEventArgs e)
        {
            ShowImageNotification(ImagePosition.Top);
        }

        private void OnShowImageBottom(object sender, RoutedEventArgs e)
        {
            ShowImageNotification(ImagePosition.Bottom);
        }

        private void ShowImageNotification(ImagePosition position)
        {
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TestImage.png");

            if (!File.Exists(imagePath))
            {
                SetLog($"Image not found: {imagePath}");
                return;
            }

            byte[] imageBytes = File.ReadAllBytes(imagePath);

            NotificationRequest request = NotificationBuilder
                .Create("Image Notification", $"Image position: {position}")
                .AsInformation()
                .ExpiresInSeconds(10)
                .WithImage(new NotificationImageData
                {
                    RawData = imageBytes,
                    Position = position
                })
                .Build();

            _service.Show(request);
        }

        private void OnShowCustomContent(object sender, RoutedEventArgs e)
        {
            // Build a rich custom content panel
            StackPanel content = new StackPanel { Spacing = 8 };

            // Custom header with icon-like element
            StackPanel headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            Border iconBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(Color.Parse("#4CAF50")),
                Child = new TextBlock
                {
                    Text = "✓",
                    FontSize = 20,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            StackPanel headerText = new StackPanel { Spacing = 2 };
            headerText.Children.Add(new TextBlock
            {
                Text = "Custom Content Demo",
                FontWeight = FontWeight.Bold,
                FontSize = 15,
                Foreground = Brushes.White
            });
            headerText.Children.Add(new TextBlock
            {
                Text = "Any Avalonia control inside!",
                FontSize = 12,
                Foreground = Brushes.White,
                Opacity = 0.8
            });

            headerPanel.Children.Add(iconBorder);
            headerPanel.Children.Add(headerText);
            content.Children.Add(headerPanel);

            // Separator line
            content.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 4)
            });

            // Rating-like stars
            StackPanel starsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };
            for (int i = 0; i < 5; i++)
            {
                starsPanel.Children.Add(new TextBlock
                {
                    Text = "★",
                    FontSize = 18,
                    Foreground = i < 4
                        ? new SolidColorBrush(Color.Parse("#FFD700"))
                        : new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
                });
            }
            content.Children.Add(starsPanel);

            // Description text
            content.Children.Add(new TextBlock
            {
                Text = "This notification uses a fully custom Avalonia layout with icons, stars, and styled text.",
                FontSize = 12,
                Foreground = Brushes.White,
                Opacity = 0.9,
                TextWrapping = TextWrapping.Wrap
            });

            _manager.ShowCustomContent(content, TimeSpan.FromSeconds(10),
                NotificationColor.FromHex("#37474F"));
        }

        #endregion

        #region Custom Notification

        private void OnShowCustom(object sender, RoutedEventArgs e)
        {
            TextBox txtTitle = this.FindControl<TextBox>("TxtTitle");
            TextBox txtMessage = this.FindControl<TextBox>("TxtMessage");
            ComboBox cmbType = this.FindControl<ComboBox>("CmbType");
            NumericUpDown numExpiration = this.FindControl<NumericUpDown>("NumExpiration");
            CheckBox chkCloseOnClick = this.FindControl<CheckBox>("ChkCloseOnClick");
            CheckBox chkLeftButton = this.FindControl<CheckBox>("ChkLeftButton");
            TextBox txtLeftButton = this.FindControl<TextBox>("TxtLeftButton");
            CheckBox chkRightButton = this.FindControl<CheckBox>("ChkRightButton");
            TextBox txtRightButton = this.FindControl<TextBox>("TxtRightButton");
            CheckBox chkCustomBg = this.FindControl<CheckBox>("ChkCustomBg");
            TextBox txtBgColor = this.FindControl<TextBox>("TxtBgColor");
            CheckBox chkCustomFg = this.FindControl<CheckBox>("ChkCustomFg");
            TextBox txtFgColor = this.FindControl<TextBox>("TxtFgColor");

            NotificationType type = GetSelectedType(cmbType?.SelectedIndex ?? 1);

            NotificationBuilder builder = NotificationBuilder
                .Create(txtTitle?.Text ?? "Title", txtMessage?.Text ?? "Message")
                .OfType(type)
                .ExpiresInSeconds((double)(numExpiration?.Value ?? 5));

            if (chkCloseOnClick?.IsChecked == true)
                builder.CloseOnClick();

            if (chkLeftButton?.IsChecked == true && !string.IsNullOrEmpty(txtLeftButton?.Text))
            {
                string leftText = txtLeftButton.Text;
                builder.WithLeftButton(leftText, () =>
                {
                    Dispatcher.UIThread.Post(() => SetLog($"Left button '{leftText}' clicked"));
                });
            }

            if (chkRightButton?.IsChecked == true && !string.IsNullOrEmpty(txtRightButton?.Text))
            {
                string rightText = txtRightButton.Text;
                builder.WithRightButton(rightText, () =>
                {
                    Dispatcher.UIThread.Post(() => SetLog($"Right button '{rightText}' clicked"));
                });
            }

            if (chkCustomBg?.IsChecked == true && !string.IsNullOrEmpty(txtBgColor?.Text))
                builder.WithBackground(txtBgColor.Text);

            if (chkCustomFg?.IsChecked == true && !string.IsNullOrEmpty(txtFgColor?.Text))
                builder.WithForeground(txtFgColor.Text);

            _service.Show(builder.Build());
        }

        private static NotificationType GetSelectedType(int index)
        {
            return index switch
            {
                0 => NotificationType.Success,
                1 => NotificationType.Information,
                2 => NotificationType.Warning,
                3 => NotificationType.Error,
                _ => NotificationType.Information
            };
        }

        #endregion

        #region Progress

        private async void OnProgressDemo(object sender, RoutedEventArgs e)
        {
            Button btnProgress = this.FindControl<Button>("BtnProgress");
            if (btnProgress != null)
                btnProgress.IsEnabled = false;

            using INotifierProgress progress = _manager.ShowProgressBar(
                "Downloading update...",
                showCancelButton: true,
                waitingMessage: "Calculating time");

            try
            {
                for (int i = 0; i <= 100; i += 2)
                {
                    progress.CancellationToken.ThrowIfCancellationRequested();
                    progress.Report(i, $"File {i / 2} of 50", null, true);
                    await Task.Delay(80);
                }

                progress.Report(100, "Download complete!", "Done", false);
                await Task.Delay(1000);
            }
            catch (OperationCanceledException)
            {
                SetLog("Progress was cancelled by user");
            }

            if (btnProgress != null)
                Dispatcher.UIThread.Post(() => btnProgress.IsEnabled = true);
        }

        private async void OnShowCustomProgress(object sender, RoutedEventArgs e)
        {
            TextBox txtProgressTitle = this.FindControl<TextBox>("TxtProgressTitle");
            CheckBox chkProgressCancel = this.FindControl<CheckBox>("ChkProgressCancel");

            string title = txtProgressTitle?.Text ?? "Processing...";
            bool showCancel = chkProgressCancel?.IsChecked ?? true;

            using INotifierProgress progress = _manager.ShowProgressBar(
                title,
                showCancelButton: showCancel,
                waitingMessage: "Calculating time");

            try
            {
                for (int i = 0; i <= 100; i++)
                {
                    progress.CancellationToken.ThrowIfCancellationRequested();
                    progress.Report(i, $"Step {i} of 100");
                    await Task.Delay(50);
                }

                progress.Report(100, "Complete!", title, false);
                await Task.Delay(1500);
            }
            catch (OperationCanceledException)
            {
                SetLog($"Progress '{title}' cancelled");
            }
        }

        #endregion

        private void SetLog(string text)
        {
            Dispatcher.UIThread.Post(() =>
            {
                TextBlock log = this.FindControl<TextBlock>("TxtLog");
                if (log != null)
                    log.Text = text;
            });
        }
    }
}
