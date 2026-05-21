using System;

namespace Notification.Core
{
    public class NotificationContentData : INotificationContent
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; } = NotificationType.None;
        public NotificationColor? BackgroundColor { get; set; }
        public NotificationColor? ForegroundColor { get; set; }
        public NotificationTextTrimType TrimType { get; set; } = NotificationTextTrimType.NoTrim;
        public uint RowsCount { get; set; } = 2;
        public NotificationTextSettings TitleSettings { get; set; }
        public NotificationTextSettings MessageSettings { get; set; }
        public object Icon { get; set; }
        public NotificationImageData Image { get; set; }
        public Action LeftButtonAction { get; set; }
        public object LeftButtonContent { get; set; }
        public Action RightButtonAction { get; set; }
        public object RightButtonContent { get; set; }
        public bool CloseOnClick { get; set; } = true;
    }
}
