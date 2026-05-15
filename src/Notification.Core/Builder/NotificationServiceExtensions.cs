using System;

namespace Notification.Core
{
    public static class NotificationServiceExtensions
    {
        public static Guid Show(this INotificationService service, Action<NotificationBuilder> configure)
        {
            NotificationBuilder builder = NotificationBuilder.Create();
            configure(builder);
            return service.Show(builder.Build());
        }
    }
}
