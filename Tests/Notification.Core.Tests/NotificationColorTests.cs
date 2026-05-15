using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationColorTests
    {
        [Fact]
        public void Constructor_SetsRGBA()
        {
            NotificationColor color = new NotificationColor(10, 20, 30, 40);

            Assert.Equal(10, color.R);
            Assert.Equal(20, color.G);
            Assert.Equal(30, color.B);
            Assert.Equal(40, color.A);
        }

        [Fact]
        public void Constructor_DefaultAlpha_Is255()
        {
            NotificationColor color = new NotificationColor(10, 20, 30);

            Assert.Equal(255, color.A);
        }

        [Fact]
        public void FromRgb_CreatesOpaqueColor()
        {
            NotificationColor color = NotificationColor.FromRgb(100, 150, 200);

            Assert.Equal(100, color.R);
            Assert.Equal(150, color.G);
            Assert.Equal(200, color.B);
            Assert.Equal(255, color.A);
        }

        [Fact]
        public void FromArgb_CreatesColorWithAlpha()
        {
            NotificationColor color = NotificationColor.FromArgb(128, 100, 150, 200);

            Assert.Equal(128, color.A);
            Assert.Equal(100, color.R);
            Assert.Equal(150, color.G);
            Assert.Equal(200, color.B);
        }

        [Theory]
        [InlineData("#FF0000", 255, 0, 0)]
        [InlineData("#00FF00", 0, 255, 0)]
        [InlineData("#0000FF", 0, 0, 255)]
        [InlineData("#FFFFFF", 255, 255, 255)]
        [InlineData("#000000", 0, 0, 0)]
        [InlineData("FF0000", 255, 0, 0)]
        public void FromHex_ParsesCorrectly(string hex, byte r, byte g, byte b)
        {
            NotificationColor color = NotificationColor.FromHex(hex);

            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        [Theory]
        [InlineData("#80FF0000", 128, 255, 0, 0)]
        public void FromHex_WithAlpha_ParsesCorrectly(string hex, byte a, byte r, byte g, byte b)
        {
            NotificationColor color = NotificationColor.FromHex(hex);

            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        [Fact]
        public void ToHex_ReturnsCorrectFormat()
        {
            NotificationColor color = new NotificationColor(255, 128, 0);

            string hex = color.ToHex();

            Assert.Equal("#FFFF8000", hex);
        }

        [Fact]
        public void ImplicitConversion_FromString()
        {
            NotificationColor color = "#FF0000";

            Assert.Equal(255, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            NotificationColor a = new NotificationColor(10, 20, 30, 40);
            NotificationColor b = new NotificationColor(10, 20, 30, 40);

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
        }

        [Fact]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            NotificationColor a = new NotificationColor(10, 20, 30);
            NotificationColor b = new NotificationColor(10, 20, 31);

            Assert.False(a.Equals(b));
            Assert.True(a != b);
        }

        [Fact]
        public void PredefinedColors_HaveExpectedValues()
        {
            Assert.Equal(255, NotificationColor.White.R);
            Assert.Equal(255, NotificationColor.White.G);
            Assert.Equal(255, NotificationColor.White.B);

            Assert.Equal(50, NotificationColor.LimeGreen.R);
            Assert.Equal(205, NotificationColor.LimeGreen.G);
            Assert.Equal(50, NotificationColor.LimeGreen.B);

            Assert.True(NotificationColor.OrangeRed.R > 200);
        }

        [Fact]
        public void GetHashCode_SameValues_SameHash()
        {
            NotificationColor a = new NotificationColor(10, 20, 30, 40);
            NotificationColor b = new NotificationColor(10, 20, 30, 40);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }
    }
}
