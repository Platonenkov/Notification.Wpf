using System;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationConfigurationTests
    {
        [Fact]
        public void DefaultConfiguration_HasExpectedDefaults()
        {
            NotificationConfiguration config = new NotificationConfiguration();

            Assert.Equal(NotificationPosition.BottomRight, config.MessagePosition);
            Assert.Equal(TimeSpan.FromSeconds(5), config.DefaultExpirationTime);
            Assert.Equal(350.0, config.MinWidth);
            Assert.Equal(350.0, config.MaxWidth);
            Assert.Equal(999u, config.MaxOverlayWindowCount);
            Assert.Equal(14.0, config.BaseTextSize);
            Assert.Equal("Segoe UI", config.FontName);
            Assert.Equal(2u, config.DefaultRowCount);
            Assert.Equal(NotificationTextTrimType.NoTrim, config.DefaultTrimType);
        }

        [Fact]
        public void DefaultConfiguration_HasColorDefaults()
        {
            NotificationConfiguration config = new NotificationConfiguration();

            Assert.Equal(NotificationColor.WhiteSmoke, config.DefaultForegroundColor);
            Assert.Equal(NotificationColor.LimeGreen, config.SuccessBackgroundColor);
            Assert.Equal(NotificationColor.Orange, config.WarningBackgroundColor);
            Assert.Equal(NotificationColor.OrangeRed, config.ErrorBackgroundColor);
            Assert.Equal(NotificationColor.CornflowerBlue, config.InformationBackgroundColor);
        }

        [Fact]
        public void Configuration_PropertiesAreWritable()
        {
            NotificationConfiguration config = new NotificationConfiguration();

            config.MessagePosition = NotificationPosition.TopLeft;
            config.DefaultExpirationTime = TimeSpan.FromSeconds(10);
            config.MinWidth = 200.0;
            config.MaxWidth = 500.0;
            config.FontName = "Arial";
            config.BaseTextSize = 16.0;

            Assert.Equal(NotificationPosition.TopLeft, config.MessagePosition);
            Assert.Equal(TimeSpan.FromSeconds(10), config.DefaultExpirationTime);
            Assert.Equal(200.0, config.MinWidth);
            Assert.Equal(500.0, config.MaxWidth);
            Assert.Equal("Arial", config.FontName);
            Assert.Equal(16.0, config.BaseTextSize);
        }

        [Fact]
        public void Configuration_ImplementsInterface()
        {
            INotificationConfiguration config = new NotificationConfiguration();

            Assert.IsAssignableFrom<INotificationConfiguration>(config);
        }
    }
}
