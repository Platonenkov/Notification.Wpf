using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationTextSettingsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            NotificationTextSettings settings = new NotificationTextSettings();

            Assert.Equal("Segoe UI", settings.FontFamily);
            Assert.Equal(14.0, settings.FontSize);
            Assert.Equal(NotificationFontStyle.Normal, settings.FontStyle);
            Assert.Equal(NotificationFontWeight.Normal, settings.FontWeight);
            Assert.Equal(NotificationTextAlignment.Left, settings.TextAlignment);
            Assert.Equal(NotificationHorizontalAlignment.Stretch, settings.HorizontalAlignment);
            Assert.Equal(NotificationVerticalAlignment.Stretch, settings.VerticalAlignment);
            Assert.Equal(1.0, settings.Opacity);
        }

        [Fact]
        public void Properties_AreSettable()
        {
            NotificationTextSettings settings = new NotificationTextSettings
            {
                FontFamily = "Arial",
                FontSize = 24.0,
                FontStyle = NotificationFontStyle.Italic,
                FontWeight = NotificationFontWeight.Bold,
                TextAlignment = NotificationTextAlignment.Center,
                HorizontalAlignment = NotificationHorizontalAlignment.Center,
                VerticalAlignment = NotificationVerticalAlignment.Center,
                Opacity = 0.5
            };

            Assert.Equal("Arial", settings.FontFamily);
            Assert.Equal(24.0, settings.FontSize);
            Assert.Equal(NotificationFontStyle.Italic, settings.FontStyle);
            Assert.Equal(NotificationFontWeight.Bold, settings.FontWeight);
            Assert.Equal(NotificationTextAlignment.Center, settings.TextAlignment);
            Assert.Equal(NotificationHorizontalAlignment.Center, settings.HorizontalAlignment);
            Assert.Equal(NotificationVerticalAlignment.Center, settings.VerticalAlignment);
            Assert.Equal(0.5, settings.Opacity);
        }
    }
}
