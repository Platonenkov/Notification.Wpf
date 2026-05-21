using System.Windows.Media;
using Notification.Core;
using Notification.Wpf.Adapters;
using Xunit;

namespace Notification.Wpf.Tests
{
    public class ColorExtensionsTests
    {
        [Fact]
        public void ToBrush_ConvertsCoreColorToSolidColorBrush()
        {
            NotificationColor color = new NotificationColor(255, 128, 0, 200);

            SolidColorBrush brush = color.ToBrush();

            Assert.Equal(255, brush.Color.R);
            Assert.Equal(128, brush.Color.G);
            Assert.Equal(0, brush.Color.B);
            Assert.Equal(200, brush.Color.A);
        }

        [Fact]
        public void ToNotificationColor_FromSolidColorBrush_Roundtrips()
        {
            NotificationColor original = new NotificationColor(100, 150, 200, 255);

            SolidColorBrush brush = original.ToBrush();
            NotificationColor result = brush.ToNotificationColor();

            Assert.Equal(original, result);
        }

        [Fact]
        public void ToNotificationColor_FromBrush_NonSolid_ReturnsDarkGray()
        {
            LinearGradientBrush gradient = new LinearGradientBrush();

            NotificationColor result = ((Brush)gradient).ToNotificationColor();

            Assert.Equal(NotificationColor.DarkGray, result);
        }

        [Fact]
        public void ToBrush_PredefinedColors_MatchExpected()
        {
            SolidColorBrush whiteBrush = NotificationColor.White.ToBrush();

            Assert.Equal(255, whiteBrush.Color.R);
            Assert.Equal(255, whiteBrush.Color.G);
            Assert.Equal(255, whiteBrush.Color.B);
        }
    }
}
