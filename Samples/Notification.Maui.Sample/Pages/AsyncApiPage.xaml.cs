using System;
using System.Threading;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Notification.Core;

namespace Notification.Maui.Sample.Pages
{
    public partial class AsyncApiPage : ContentPage
    {
        private INotificationService _service;
        private CancellationTokenSource _ctsDemo4;

        public AsyncApiPage()
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
        }

        private void SetResult(Label label, string text)
        {
            MainThread.BeginInvokeOnMainThread(() => label.Text = text);
        }

        #region Demo 1 — ShowAsync simple

        private async void OnAsyncSuccess(object sender, EventArgs e)
        {
            SetResult(LblAsyncResult, "⏳ Waiting for close…");
            try
            {
                NotificationCloseResult result = await _service.ShowAsync(
                    "Saved", "Your data has been saved successfully.",
                    NotificationType.Success,
                    expirationTime: TimeSpan.FromSeconds(4));

                SetResult(LblAsyncResult,
                    $"Reason={result.Reason} | ButtonId={result.ButtonId ?? "—"}");
            }
            catch (Exception ex)
            {
                SetResult(LblAsyncResult, $"Exception: {ex.Message}");
            }
        }

        private async void OnAsyncInfo(object sender, EventArgs e)
        {
            SetResult(LblAsyncResult, "⏳ Waiting for close…");
            try
            {
                NotificationCloseResult result = await _service.ShowAsync(
                    "Info", "New version v11.0 available.",
                    NotificationType.Information,
                    expirationTime: TimeSpan.FromSeconds(4));

                SetResult(LblAsyncResult,
                    $"Reason={result.Reason} | ButtonId={result.ButtonId ?? "—"}");
            }
            catch (Exception ex)
            {
                SetResult(LblAsyncResult, $"Exception: {ex.Message}");
            }
        }

        #endregion

        #region Demo 2 — ShowDialogAsync 2 buttons

        private async void OnDialog2Buttons(object sender, EventArgs e)
        {
            SetResult(LblDialog2Result, "⏳ Waiting for choice…");
            try
            {
                NotificationCloseResult result = await _service.ShowDialogAsync(
                    new NotificationRequest
                    {
                        Title = "Confirm Delete",
                        Message = "This action cannot be undone. Are you sure?",
                        Type = NotificationType.Warning,
                        ExpirationTime = TimeSpan.FromSeconds(10),
                        Buttons = new[]
                        {
                            new NotificationButton { Id = "delete", Content = "Delete", Role = NotificationButtonRole.Affirmative },
                            new NotificationButton { Id = "cancel", Content = "Cancel", Role = NotificationButtonRole.Negative }
                        }
                    });

                SetResult(LblDialog2Result,
                    $"Reason={result.Reason} | ButtonId={result.ButtonId ?? "—"} | Index={result.ButtonIndex?.ToString() ?? "—"}");
            }
            catch (Exception ex)
            {
                SetResult(LblDialog2Result, $"Exception: {ex.Message}");
            }
        }

        #endregion

        #region Demo 3 — ShowDialogAsync 3 buttons + fallback
        // MAUI Snackbar supports at most one action button.
        // ShowFirstAffirmative: shows the first Affirmative button; result carries its ButtonId.
        // SkipSilently:         no snackbar shown; result has Reason = Timeout immediately.
        // Throw:                throws NotSupportedException synchronously.

        private async void OnDialog3Buttons(object sender, EventArgs e)
        {
            SetResult(LblDialog3Result, "⏳ Running…");

            NotificationDialogFallback fallback = PkrFallback.SelectedIndex switch
            {
                1 => NotificationDialogFallback.SkipSilently,
                2 => NotificationDialogFallback.Throw,
                _ => NotificationDialogFallback.ShowFirstAffirmative
            };

            try
            {
                NotificationCloseResult result = await _service.ShowDialogAsync(
                    new NotificationRequest
                    {
                        Title = "Choose wisely",
                        Message = "What do you want to do?",
                        Type = NotificationType.Information,
                        ExpirationTime = TimeSpan.FromSeconds(10),
                        Buttons = new[]
                        {
                            new NotificationButton { Id = "yes",   Content = "Yes",   Role = NotificationButtonRole.Affirmative },
                            new NotificationButton { Id = "no",    Content = "No",    Role = NotificationButtonRole.Negative },
                            new NotificationButton { Id = "maybe", Content = "Maybe", Role = NotificationButtonRole.Default }
                        }
                    },
                    fallback);

                SetResult(LblDialog3Result,
                    $"Reason={result.Reason} | ButtonId={result.ButtonId ?? "—"} | Index={result.ButtonIndex?.ToString() ?? "—"}");
            }
            catch (NotSupportedException ex)
            {
                SetResult(LblDialog3Result, $"NotSupportedException (Throw): {ex.Message}");
            }
            catch (Exception ex)
            {
                SetResult(LblDialog3Result, $"Exception: {ex.Message}");
            }
        }

        #endregion

        #region Demo 4 — CancellationToken

        private async void OnStartCancellable(object sender, EventArgs e)
        {
            _ctsDemo4?.Cancel();
            _ctsDemo4?.Dispose();
            _ctsDemo4 = new CancellationTokenSource();

            BtnStartCt.IsEnabled = false;
            BtnCancelCt.IsEnabled = true;
            SetResult(LblCtResult, "⏳ Snackbar visible for 10 sec — press Cancel to dismiss early…");

            try
            {
                NotificationCloseResult result = await _service.ShowAsync(
                    new NotificationRequest
                    {
                        Title = "Pending",
                        Message = "Long-running operation. Press Cancel to abort.",
                        Type = NotificationType.Information,
                        ExpirationTime = TimeSpan.FromSeconds(10)
                    },
                    _ctsDemo4.Token);

                SetResult(LblCtResult,
                    $"Reason={result.Reason} | IsCancelled={result.IsCancelled} | ButtonId={result.ButtonId ?? "—"}");
            }
            catch (OperationCanceledException)
            {
                SetResult(LblCtResult, "Caught OperationCanceledException");
            }
            catch (Exception ex)
            {
                SetResult(LblCtResult, $"Exception: {ex.Message}");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BtnStartCt.IsEnabled = true;
                    BtnCancelCt.IsEnabled = false;
                });
            }
        }

        private void OnCancelCancellable(object sender, EventArgs e)
        {
            _ctsDemo4?.Cancel();
        }

        #endregion

        private async void OnBack(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
