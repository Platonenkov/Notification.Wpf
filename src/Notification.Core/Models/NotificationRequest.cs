using System;
using System.Collections.Generic;

namespace Notification.Core
{
    public sealed class NotificationRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; } = NotificationType.None;

        public string AreaName { get; set; } = "";
        public TimeSpan? ExpirationTime { get; set; }
        public bool CloseOnClick { get; set; } = true;
        public bool ShowCloseButton { get; set; } = true;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public string GroupKey { get; set; }

        public NotificationColor? BackgroundColor { get; set; }
        public NotificationColor? ForegroundColor { get; set; }

        public NotificationTextSettings TitleSettings { get; set; }
        public NotificationTextSettings MessageSettings { get; set; }
        public NotificationTextTrimType TrimType { get; set; } = NotificationTextTrimType.NoTrim;
        public uint RowsCount { get; set; } = 2;

        public Action LeftButtonAction { get; set; }
        public string LeftButtonContent { get; set; }
        public Action RightButtonAction { get; set; }
        public string RightButtonContent { get; set; }

        public Action OnClick { get; set; }
        public Action OnClose { get; set; }

        public object Icon { get; set; }
        public object PlatformImage { get; set; }

        public IDictionary<string, object> Extensions { get; set; }
    }
}
