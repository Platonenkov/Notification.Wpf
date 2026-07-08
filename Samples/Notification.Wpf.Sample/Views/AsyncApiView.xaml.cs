using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Notification.Wpf.Sample.Views
{
    public partial class AsyncApiView : UserControl
    {
        private CancellationTokenSource _ctsDemo4;

        private INotificationService NotificationService =>
            App.Services.GetRequiredService<INotificationService>();

        public AsyncApiView()
        {
            InitializeComponent();
        }

        #region Demo 1 — ShowAsync simple

        private async void OnShowAsync(object sender, RoutedEventArgs e)
        {
            TxtShowAsyncResult.Text = "⏳ Waiting for close…";
            try
            {
                NotificationCloseResult result = await NotificationService.ShowAsync(
                    "Saved", "Your data has been saved successfully.",
                    NotificationType.Success,
                    expirationTime: TimeSpan.FromSeconds(6));

                TxtShowAsyncResult.Text = $"Reason = {result.Reason}  |  ButtonId = {result.ButtonId ?? "—"}";
            }
            catch (Exception ex)
            {
                TxtShowAsyncResult.Text = $"Exception: {ex.Message}";
            }
        }

        private async void OnShowAsyncWarning(object sender, RoutedEventArgs e)
        {
            TxtShowAsyncResult.Text = "⏳ Waiting for close…";
            try
            {
                NotificationCloseResult result = await NotificationService.ShowAsync(
                    "Low disk space", "Less than 500 MB remaining.",
                    NotificationType.Warning,
                    expirationTime: TimeSpan.FromSeconds(4));

                TxtShowAsyncResult.Text = $"Reason = {result.Reason}  |  ButtonId = {result.ButtonId ?? "—"}";
            }
            catch (Exception ex)
            {
                TxtShowAsyncResult.Text = $"Exception: {ex.Message}";
            }
        }

        #endregion

        #region Demo 2 — ShowDialogAsync 2 buttons

        private async void OnShowDialog2Buttons(object sender, RoutedEventArgs e)
        {
            TxtDialog2Result.Text = "⏳ Waiting for choice…";
            try
            {
                NotificationCloseResult result = await NotificationService.ShowDialogAsync(
                    new NotificationRequest
                    {
                        Title = "Confirm Delete",
                        Message = "This action cannot be undone. Are you sure?",
                        Type = NotificationType.Warning,
                        ExpirationTime = TimeSpan.FromSeconds(20),
                        Buttons = new[]
                        {
                            new NotificationButton { Id = "delete", Content = "Delete", Role = NotificationButtonRole.Affirmative },
                            new NotificationButton { Id = "cancel", Content = "Cancel", Role = NotificationButtonRole.Negative }
                        }
                    });

                TxtDialog2Result.Text = $"Reason = {result.Reason}  |  ButtonId = {result.ButtonId ?? "—"}  |  ButtonIndex = {result.ButtonIndex?.ToString() ?? "—"}";
            }
            catch (Exception ex)
            {
                TxtDialog2Result.Text = $"Exception: {ex.Message}";
            }
        }

        #endregion

        #region Demo 3 — ShowDialogAsync 3 buttons + fallback

        private async void OnShowDialog3Buttons(object sender, RoutedEventArgs e)
        {
            TxtDialog3Result.Text = "⏳ Waiting for choice…";

            NotificationDialogFallback fallback = CmbFallback.SelectedIndex switch
            {
                1 => NotificationDialogFallback.SkipSilently,
                2 => NotificationDialogFallback.Throw,
                _ => NotificationDialogFallback.ShowFirstAffirmative
            };

            try
            {
                NotificationCloseResult result = await NotificationService.ShowDialogAsync(
                    new NotificationRequest
                    {
                        Title = "Choose wisely",
                        Message = "What do you want to do?",
                        Type = NotificationType.Information,
                        ExpirationTime = TimeSpan.FromSeconds(20),
                        Buttons = new[]
                        {
                            new NotificationButton { Id = "yes",   Content = "Yes",   Role = NotificationButtonRole.Affirmative },
                            new NotificationButton { Id = "no",    Content = "No",    Role = NotificationButtonRole.Negative },
                            new NotificationButton { Id = "maybe", Content = "Maybe", Role = NotificationButtonRole.Default }
                        }
                    },
                    fallback);

                TxtDialog3Result.Text = $"Reason = {result.Reason}  |  ButtonId = {result.ButtonId ?? "—"}  |  ButtonIndex = {result.ButtonIndex?.ToString() ?? "—"}";
            }
            catch (NotSupportedException ex)
            {
                TxtDialog3Result.Text = $"NotSupportedException (Throw fallback): {ex.Message}";
            }
            catch (Exception ex)
            {
                TxtDialog3Result.Text = $"Exception: {ex.Message}";
            }
        }

        #endregion

        #region Demo 4 — CancellationToken

        private async void OnStartCancellable(object sender, RoutedEventArgs e)
        {
            _ctsDemo4?.Cancel();
            _ctsDemo4?.Dispose();
            _ctsDemo4 = new CancellationTokenSource();

            BtnStartCt.IsEnabled = false;
            BtnCancelCt.IsEnabled = true;
            TxtCtResult.Text = "⏳ Notification visible for 60 sec — press ✕ to cancel…";

            try
            {
                NotificationCloseResult result = await NotificationService.ShowAsync(
                    new NotificationRequest
                    {
                        Title = "Pending Operation",
                        Message = "This notification will stay for 60 seconds. Click ✕ Отменить to cancel early.",
                        Type = NotificationType.Information,
                        ExpirationTime = TimeSpan.FromSeconds(60)
                    },
                    _ctsDemo4.Token);

                TxtCtResult.Text = $"Reason = {result.Reason}  |  IsCancelled = {result.IsCancelled}  |  ButtonId = {result.ButtonId ?? "—"}";
            }
            catch (OperationCanceledException)
            {
                TxtCtResult.Text = "Caught OperationCanceledException (token was already cancelled before Show)";
            }
            catch (Exception ex)
            {
                TxtCtResult.Text = $"Exception: {ex.Message}";
            }
            finally
            {
                BtnStartCt.IsEnabled = true;
                BtnCancelCt.IsEnabled = false;
            }
        }

        private void OnCancelCancellable(object sender, RoutedEventArgs e)
        {
            _ctsDemo4?.Cancel();
        }

        #endregion
    }
}
