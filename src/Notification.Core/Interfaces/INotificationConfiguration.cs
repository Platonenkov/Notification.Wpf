using System;

namespace Notification.Core
{
    public interface INotificationConfiguration
    {
        NotificationPosition MessagePosition { get; set; }
        bool? IsReversedPanel { get; set; }
        double MinWidth { get; set; }
        double MaxWidth { get; set; }
        uint MaxOverlayWindowCount { get; set; }
        bool CollapseProgressIfMoreRows { get; set; }
        TimeSpan DefaultExpirationTime { get; set; }

        NotificationColor DefaultBackgroundColor { get; set; }
        NotificationColor DefaultForegroundColor { get; set; }
        NotificationColor SuccessBackgroundColor { get; set; }
        NotificationColor WarningBackgroundColor { get; set; }
        NotificationColor ErrorBackgroundColor { get; set; }
        NotificationColor InformationBackgroundColor { get; set; }
        NotificationColor DefaultProgressColor { get; set; }

        double BaseTextSize { get; set; }
        double TitleSize { get; set; }
        double MessageSize { get; set; }
        string FontName { get; set; }
        NotificationTextAlignment TitleTextAlignment { get; set; }
        NotificationTextAlignment MessageTextAlignment { get; set; }
        uint DefaultRowCount { get; set; }
        NotificationTextTrimType DefaultTrimType { get; set; }

        string CancellationMessage { get; set; }
        string OpenFileMessage { get; set; }
        string OpenFolderMessage { get; set; }
        string DefaultLeftButtonContent { get; set; }
        string DefaultRightButtonContent { get; set; }
        string DefaultProgressButtonContent { get; set; }
    }
}
