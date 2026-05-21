using System;

namespace Notification.Core
{
    public interface INotificationQueue
    {
        Guid Enqueue(NotificationRequest request);
        int PendingCount { get; }
    }
}
