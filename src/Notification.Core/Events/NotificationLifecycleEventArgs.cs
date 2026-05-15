using System;

namespace Notification.Core
{
    public sealed class NotificationLifecycleEventArgs : EventArgs
    {
        public Guid NotificationId { get; }
        public NotificationLifecycleStage Stage { get; }
        public DateTimeOffset Timestamp { get; }
        public string Title { get; }
        public string Message { get; }

        public NotificationLifecycleEventArgs(
            Guid notificationId,
            NotificationLifecycleStage stage,
            string title,
            string message)
        {
            NotificationId = notificationId;
            Stage = stage;
            Timestamp = DateTimeOffset.UtcNow;
            Title = title;
            Message = message;
        }
    }
}
