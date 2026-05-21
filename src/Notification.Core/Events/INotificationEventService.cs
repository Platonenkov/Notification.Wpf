using System;

namespace Notification.Core
{
    public interface INotificationEventService
    {
        event EventHandler<NotificationLifecycleEventArgs> NotificationLifecycleChanged;
        event EventHandler<NotificationLifecycleEventArgs> NotificationShown;
        event EventHandler<NotificationLifecycleEventArgs> NotificationClosed;
        event EventHandler<NotificationLifecycleEventArgs> NotificationClicked;

        void Raise(NotificationLifecycleEventArgs args);
    }
}
