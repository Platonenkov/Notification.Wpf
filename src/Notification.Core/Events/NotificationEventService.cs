using System;

namespace Notification.Core
{
    public class NotificationEventService : INotificationEventService
    {
        public event EventHandler<NotificationLifecycleEventArgs> NotificationLifecycleChanged;
        public event EventHandler<NotificationLifecycleEventArgs> NotificationShown;
        public event EventHandler<NotificationLifecycleEventArgs> NotificationClosed;
        public event EventHandler<NotificationLifecycleEventArgs> NotificationClicked;

        public void Raise(NotificationLifecycleEventArgs args)
        {
            NotificationLifecycleChanged?.Invoke(this, args);

            switch (args.Stage)
            {
                case NotificationLifecycleStage.Shown:
                    NotificationShown?.Invoke(this, args);
                    break;
                case NotificationLifecycleStage.Clicked:
                    NotificationClicked?.Invoke(this, args);
                    break;
                case NotificationLifecycleStage.Closed:
                case NotificationLifecycleStage.TimedOut:
                case NotificationLifecycleStage.Dismissed:
                    NotificationClosed?.Invoke(this, args);
                    break;
            }
        }
    }
}
