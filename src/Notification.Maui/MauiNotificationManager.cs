using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Notification.Core;

#if ANDROID || IOS || MACCATALYST || WINDOWS
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
#endif

namespace Notification.Maui
{
    public class MauiNotificationManager : INotificationService
    {
        private readonly INotificationConfiguration _config;
        private readonly INotificationEventService _events;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeNotifications =
            new ConcurrentDictionary<Guid, CancellationTokenSource>();

        public MauiNotificationManager()
        {
        }

        public MauiNotificationManager(INotificationConfiguration config, INotificationEventService events)
        {
            _config = config;
            _events = events;
        }

        public Guid Show(NotificationRequest request)
        {
            Guid id = request.Id;
            CancellationTokenSource cts = new CancellationTokenSource();
            _activeNotifications.TryAdd(id, cts);

            TimeSpan expiration = request.ExpirationTime ?? _config?.DefaultExpirationTime ?? TimeSpan.FromSeconds(5);

#if ANDROID || IOS || MACCATALYST || WINDOWS
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowSnackbar(request, id, cts.Token, expiration);
            });
#else
            ShowFallback(request, id);
#endif

            _events?.Raise(new NotificationLifecycleEventArgs(
                id, NotificationLifecycleStage.Shown, request.Title, request.Message));

            return id;
        }

#if ANDROID || IOS || MACCATALYST || WINDOWS
        private async void ShowSnackbar(NotificationRequest request, Guid id, CancellationToken cancellationToken, TimeSpan expiration)
        {
            try
            {
                Page anchor = GetCurrentPage();
                if (anchor == null)
                    return;

                string typePrefix = GetTypePrefix(request.Type);
                string displayText = string.IsNullOrEmpty(request.Title)
                    ? $"{typePrefix}{request.Message ?? ""}"
                    : $"{typePrefix}{request.Title}: {request.Message}";

                string actionText = request.LeftButtonContent ?? (request.OnClick != null ? "OK" : null);

                Action clickAction = actionText != null
                    ? () =>
                    {
                        request.OnClick?.Invoke();
                        request.LeftButtonAction?.Invoke();
                        _events?.Raise(new NotificationLifecycleEventArgs(
                            id, NotificationLifecycleStage.Clicked, request.Title, request.Message));
                    }
                    : null;

                SnackbarOptions options = CreateSnackbarOptions(request);

                if (actionText != null)
                {
                    ISnackbar snackbar = Snackbar.Make(displayText, clickAction, actionText, expiration, options);
                    await snackbar.Show(cancellationToken);
                }
                else
                {
                    IToast toast = Toast.Make(displayText, ToastDuration.Short);
                    await toast.Show(cancellationToken);
                }

                CancellationTokenSource removed;
                _activeNotifications.TryRemove(id, out removed);
                removed?.Dispose();

                request.OnClose?.Invoke();
                _events?.Raise(new NotificationLifecycleEventArgs(
                    id, NotificationLifecycleStage.Closed, request.Title, request.Message));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private SnackbarOptions CreateSnackbarOptions(NotificationRequest request)
        {
            Color backgroundColor = GetBackgroundColor(request);
            Color foregroundColor = GetForegroundColor(request);

            return new SnackbarOptions
            {
                BackgroundColor = backgroundColor,
                TextColor = foregroundColor,
                ActionButtonTextColor = foregroundColor,
                Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, Microsoft.Maui.FontWeight.Bold),
                CornerRadius = 8
            };
        }

        private static string GetTypePrefix(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "✅ ",
                NotificationType.Warning => "⚠️ ",
                NotificationType.Error => "❌ ",
                NotificationType.Information => "ℹ️ ",
                _ => ""
            };
        }

        private Color GetBackgroundColor(NotificationRequest request)
        {
            if (request.BackgroundColor.HasValue)
            {
                NotificationColor c = request.BackgroundColor.Value;
                return Color.FromRgba(c.R, c.G, c.B, c.A);
            }

            return request.Type switch
            {
                NotificationType.Success => Color.FromArgb("#2E7D32"),
                NotificationType.Warning => Color.FromArgb("#F57F17"),
                NotificationType.Error => Color.FromArgb("#C62828"),
                NotificationType.Information => Color.FromArgb("#1565C0"),
                _ => Color.FromArgb("#424242")
            };
        }

        private Color GetForegroundColor(NotificationRequest request)
        {
            if (request.ForegroundColor.HasValue)
            {
                NotificationColor c = request.ForegroundColor.Value;
                return Color.FromRgba(c.R, c.G, c.B, c.A);
            }

            return Colors.White;
        }

        private static Page GetCurrentPage()
        {
            Application app = Application.Current;
            if (app == null)
                return null;

            Window window = app.Windows.Count > 0 ? app.Windows[0] : null;
            Page rootPage = window?.Page;

            if (rootPage is Shell shell)
                return shell.CurrentPage;

            if (rootPage is NavigationPage navPage)
                return navPage.CurrentPage;

            return rootPage;
        }
#else
        private void ShowFallback(NotificationRequest request, Guid id)
        {
            string prefix = request.Type switch
            {
                NotificationType.Success => "[OK]",
                NotificationType.Warning => "[WARN]",
                NotificationType.Error => "[ERR]",
                NotificationType.Information => "[INFO]",
                _ => "[NOTE]"
            };
            System.Diagnostics.Debug.WriteLine($"{prefix} {request.Title}: {request.Message}");
        }
#endif

        public void Dismiss(Guid notificationId)
        {
            CancellationTokenSource cts;
            if (_activeNotifications.TryRemove(notificationId, out cts))
            {
                cts?.Cancel();
                cts?.Dispose();
                _events?.Raise(new NotificationLifecycleEventArgs(
                    notificationId, NotificationLifecycleStage.Dismissed, null, null));
            }
        }

        public void DismissAll()
        {
            foreach (KeyValuePair<Guid, CancellationTokenSource> kvp in _activeNotifications.ToArray())
            {
                Dismiss(kvp.Key);
            }
        }
    }
}
