using System.Windows.Media;
using Notification.Core;

namespace Notification.Wpf.Adapters
{
    public static class ColorExtensions
    {
        public static SolidColorBrush ToBrush(this NotificationColor color) =>
            new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));

        public static NotificationColor ToNotificationColor(this SolidColorBrush brush) =>
            new NotificationColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A);

        public static NotificationColor ToNotificationColor(this Brush brush)
        {
            if (brush is SolidColorBrush scb)
                return new NotificationColor(scb.Color.R, scb.Color.G, scb.Color.B, scb.Color.A);
            return NotificationColor.DarkGray;
        }
    }
}
