namespace Notification.Core
{
    public class NotificationTextSettings
    {
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 14.0;
        public NotificationFontStyle FontStyle { get; set; } = NotificationFontStyle.Normal;
        public NotificationFontWeight FontWeight { get; set; } = NotificationFontWeight.Normal;
        public NotificationTextAlignment TextAlignment { get; set; } = NotificationTextAlignment.Left;
        public NotificationHorizontalAlignment HorizontalAlignment { get; set; } = NotificationHorizontalAlignment.Stretch;
        public NotificationVerticalAlignment VerticalAlignment { get; set; } = NotificationVerticalAlignment.Stretch;
        public double Opacity { get; set; } = 1.0;
    }
}
