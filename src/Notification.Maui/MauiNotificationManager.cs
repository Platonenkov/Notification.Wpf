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

                string displayText = string.IsNullOrEmpty(request.Title)
                    ? request.Message ?? ""
                    : $"{request.Title}: {request.Message}";

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

                ISnackbar snackbar = Snackbar.Make(
                    displayText,
                    clickAction,
                    actionText ?? string.Empty,
                    expiration);

                await snackbar.Show(cancellationToken);

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
