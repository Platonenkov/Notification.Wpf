using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Notification.Core;

namespace Notification.Maui.Sample
{
    public partial class MainPage : ContentPage
    {
        private INotificationService _service;
        private INotificationEventService _events;

        public MainPage()
        {
            InitializeComponent();
            HandlerChanged += OnHandlerChanged;
        }

        private void OnHandlerChanged(object sender, EventArgs e)
        {
            if (Handler?.MauiContext?.Services == null)
                return;

            HandlerChanged -= OnHandlerChanged;

            _service = Handler.MauiContext.Services.GetService(typeof(INotificationService)) as INotificationService
                       ?? new MauiNotificationManager();
            _events = Handler.MauiContext.Services.GetService(typeof(INotificationEventService)) as INotificationEventService;

            if (_events != null)
            {
                _events.NotificationLifecycleChanged += OnNotificationLifecycleChanged;
            }
        }

        private void OnNotificationLifecycleChanged(object sender, NotificationLifecycleEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LblLog.Text = $"Event: {e.Stage} - {e.Title ?? "N/A"}";
            });
        }

        private void OnShowSuccess(object sender, EventArgs e)
        {
            _service?.Show(NotificationBuilder.Create("Success", "Operation completed!").AsSuccess().Build());
        }

        private void OnShowWarning(object sender, EventArgs e)
        {
            _service?.Show(NotificationBuilder.Create("Warning", "Disk space is low").AsWarning().Build());
        }

        private void OnShowError(object sender, EventArgs e)
        {
            _service?.Show(NotificationBuilder.Create("Error", "Connection failed").AsError().Build());
        }

        private void OnShowInfo(object sender, EventArgs e)
        {
            _service?.Show(NotificationBuilder.Create("Info", "New version available").AsInformation().Build());
        }

        private void OnBuilderDemo(object sender, EventArgs e)
        {
            _service?.Show(NotificationBuilder
                .Create()
                .WithTitle("Builder Demo")
                .WithMessage("Cross-platform notification via Core Builder API")
                .AsSuccess()
                .ExpiresInSeconds(5)
                .WithPriority(NotificationPriority.High)
                .Build());
        }

        private void OnShowWithButton(object sender, EventArgs e)
        {
            _service?.Show(NotificationBuilder
                .Create("Update Available", "Version 2.0 is ready to install")
                .AsInformation()
                .ExpiresInSeconds(10)
                .WithLeftButton("Install", () =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LblLog.Text = "Install button clicked!";
                    });
                })
                .Build());
        }

        private async void OnProgressDemo(object sender, EventArgs e)
        {
            BtnProgress.IsEnabled = false;
            ProgressBarDemo.IsVisible = true;
            ProgressBarDemo.Progress = 0;
            LblProgress.IsVisible = true;
            LblProgress.Text = "Processing...";

            for (int i = 0; i <= 100; i += 2)
            {
                await ProgressBarDemo.ProgressTo(i / 100.0, 50, Easing.Linear);
                LblProgress.Text = $"Processing... {i}%";
                await Task.Delay(50);
            }

            LblProgress.Text = "Done!";
            ProgressBarDemo.IsVisible = false;
            LblProgress.IsVisible = false;
            BtnProgress.IsEnabled = true;

            _service?.Show(NotificationBuilder
                .Create("Complete", "Processing finished successfully")
                .AsSuccess()
                .ExpiresInSeconds(3)
                .Build());
        }

        private void OnDismissAll(object sender, EventArgs e)
        {
            _service?.DismissAll();
            LblLog.Text = "All notifications dismissed";
        }
    }
}
