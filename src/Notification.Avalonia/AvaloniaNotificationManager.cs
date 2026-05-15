using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Notification.Core;

using AvaloniaNotificationType = Avalonia.Controls.Notifications.NotificationType;

namespace Notification.Avalonia
{
    public class AvaloniaNotificationManager : INotificationService
    {
        private readonly INotificationConfiguration _config;
        private readonly INotificationEventService _events;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeNotifications =
            new ConcurrentDictionary<Guid, CancellationTokenSource>();

        private WindowNotificationManager _windowManager;
        private readonly object _initLock = new object();

        public AvaloniaNotificationManager()
        {
        }

        public AvaloniaNotificationManager(INotificationConfiguration config, INotificationEventService events)
        {
            _config = config;
            _events = events;
        }

        public void SetHost(TopLevel host, global::Avalonia.Controls.Notifications.NotificationPosition position =
            global::Avalonia.Controls.Notifications.NotificationPosition.BottomRight)
        {
            _windowManager = new WindowNotificationManager(host)
            {
                Position = position,
                MaxItems = (int)(_config?.MaxOverlayWindowCount ?? 5)
            };
        }

        public Guid Show(NotificationRequest request)
        {
            Guid id = request.Id;

            if (_windowManager == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Notification.Avalonia] No host set. Call SetHost() first. Notification: {request.Title}");
                return id;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            _activeNotifications.TryAdd(id, cts);

            Dispatcher.UIThread.Post(() =>
            {
                AvaloniaNotificationType avaloniaType = MapNotificationType(request.Type);
                TimeSpan expiration = request.ExpirationTime ?? _config?.DefaultExpirationTime ?? TimeSpan.FromSeconds(5);

                global::Avalonia.Controls.Notifications.Notification notification =
                    new global::Avalonia.Controls.Notifications.Notification(
                        request.Title ?? "",
                        request.Message ?? "",
                        avaloniaType,
                        expiration,
                        () =>
                        {
                            CancellationTokenSource removed;
                            _activeNotifications.TryRemove(id, out removed);
                            removed?.Dispose();

                            request.OnClick?.Invoke();
                            _events?.Raise(new NotificationLifecycleEventArgs(
                                id, NotificationLifecycleStage.Clicked, request.Title, request.Message));
                        },
                        () =>
                        {
                            CancellationTokenSource removed;
                            _activeNotifications.TryRemove(id, out removed);
                            removed?.Dispose();

                            request.OnClose?.Invoke();
                            _events?.Raise(new NotificationLifecycleEventArgs(
                                id, NotificationLifecycleStage.Closed, request.Title, request.Message));
                        });

                _windowManager.Show(notification);

                _events?.Raise(new NotificationLifecycleEventArgs(
                    id, NotificationLifecycleStage.Shown, request.Title, request.Message));
            });

            return id;
        }

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

        private static AvaloniaNotificationType MapNotificationType(Core.NotificationType type)
        {
            switch (type)
            {
                case Core.NotificationType.Success: return AvaloniaNotificationType.Success;
                case Core.NotificationType.Warning: return AvaloniaNotificationType.Warning;
                case Core.NotificationType.Error: return AvaloniaNotificationType.Error;
                case Core.NotificationType.Information: return AvaloniaNotificationType.Information;
                default: return AvaloniaNotificationType.Information;
            }
        }
    }
}
