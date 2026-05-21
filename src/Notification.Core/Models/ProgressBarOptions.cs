namespace Notification.Core
{
    public class ProgressBarOptions
    {
        public string Title { get; set; }
        public bool ShowCancelButton { get; set; } = true;
        public bool ShowProgress { get; set; } = true;
        public string AreaName { get; set; } = "";
        public bool TrimText { get; set; }
        public uint DefaultRowsCount { get; set; } = 1;
        public string BaseWaitingMessage { get; set; } = "Calculation time";
        public bool IsCollapse { get; set; }
        public bool TitleWhenCollapsed { get; set; } = true;
        public NotificationColor? BackgroundColor { get; set; }
        public NotificationColor? ForegroundColor { get; set; }
        public NotificationColor? ProgressColor { get; set; }
        public object Icon { get; set; }
        public NotificationTextSettings TitleSettings { get; set; }
        public NotificationTextSettings MessageSettings { get; set; }
        public bool ShowCloseButton { get; set; } = true;
    }
}
