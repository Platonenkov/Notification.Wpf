using System;

namespace Notification.Core
{
    public interface INotificationContent
    {
        string Title { get; }
        string Message { get; }
        NotificationType Type { get; }
        NotificationColor? BackgroundColor { get; }
        NotificationColor? ForegroundColor { get; }
        NotificationTextTrimType TrimType { get; }
        uint RowsCount { get; }
        NotificationTextSettings TitleSettings { get; }
        NotificationTextSettings MessageSettings { get; }
        object Icon { get; }
        NotificationImageData Image { get; }
        Action LeftButtonAction { get; }
        object LeftButtonContent { get; }
        Action RightButtonAction { get; }
        object RightButtonContent { get; }
        bool CloseOnClick { get; }
    }
}
