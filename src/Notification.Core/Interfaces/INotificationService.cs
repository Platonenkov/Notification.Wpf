using System;

namespace Notification.Core
{
    public interface INotificationService
    {
        Guid Show(NotificationRequest request);
        void Dismiss(Guid notificationId);
        void DismissAll();
    }
}
