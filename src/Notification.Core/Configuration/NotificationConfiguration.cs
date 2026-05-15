using System;

namespace Notification.Core
{
    public class NotificationConfiguration : INotificationConfiguration
    {
        public NotificationPosition MessagePosition { get; set; } = NotificationPosition.BottomRight;
        public bool? IsReversedPanel { get; set; }
        public double MinWidth { get; set; } = 350.0;
        public double MaxWidth { get; set; } = 350.0;
        public uint MaxOverlayWindowCount { get; set; } = 999;
        public bool CollapseProgressIfMoreRows { get; set; } = true;
        public TimeSpan DefaultExpirationTime { get; set; } = TimeSpan.FromSeconds(5);

        public NotificationColor DefaultBackgroundColor { get; set; } = NotificationColor.FromHex("#FF444444");
        public NotificationColor DefaultForegroundColor { get; set; } = NotificationColor.WhiteSmoke;
        public NotificationColor SuccessBackgroundColor { get; set; } = NotificationColor.LimeGreen;
        public NotificationColor WarningBackgroundColor { get; set; } = NotificationColor.Orange;
        public NotificationColor ErrorBackgroundColor { get; set; } = NotificationColor.OrangeRed;
        public NotificationColor InformationBackgroundColor { get; set; } = NotificationColor.CornflowerBlue;
        public NotificationColor DefaultProgressColor { get; set; } = NotificationColor.Green;

        public double BaseTextSize { get; set; } = 14.0;
        public double TitleSize { get; set; } = 14.0;
        public double MessageSize { get; set; } = 14.0;
        public string FontName { get; set; } = "Segoe UI";
        public NotificationTextAlignment TitleTextAlignment { get; set; } = NotificationTextAlignment.Left;
        public NotificationTextAlignment MessageTextAlignment { get; set; } = NotificationTextAlignment.Left;
        public uint DefaultRowCount { get; set; } = 2;
        public NotificationTextTrimType DefaultTrimType { get; set; } = NotificationTextTrimType.NoTrim;

        public string CancellationMessage { get; set; } = "Operation was cancelled";
        public string OpenFileMessage { get; set; } = "Open File";
        public string OpenFolderMessage { get; set; } = "Open Folder";
        public string DefaultLeftButtonContent { get; set; } = "Ok";
        public string DefaultRightButtonContent { get; set; } = "Cancel";
        public string DefaultProgressButtonContent { get; set; } = "Cancel";
    }
}
