using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationThicknessTests
    {
        [Fact]
        public void UniformConstructor_SetsAllSides()
        {
            NotificationThickness t = new NotificationThickness(5.0);

            Assert.Equal(5.0, t.Left);
            Assert.Equal(5.0, t.Top);
            Assert.Equal(5.0, t.Right);
            Assert.Equal(5.0, t.Bottom);
        }

        [Fact]
        public void FourSideConstructor_SetsEachSide()
        {
            NotificationThickness t = new NotificationThickness(1.0, 2.0, 3.0, 4.0);

            Assert.Equal(1.0, t.Left);
            Assert.Equal(2.0, t.Top);
            Assert.Equal(3.0, t.Right);
            Assert.Equal(4.0, t.Bottom);
        }

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            NotificationThickness a = new NotificationThickness(1.0, 2.0, 3.0, 4.0);
            NotificationThickness b = new NotificationThickness(1.0, 2.0, 3.0, 4.0);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            NotificationThickness a = new NotificationThickness(1.0);
            NotificationThickness b = new NotificationThickness(2.0);

            Assert.False(a == b);
            Assert.True(a != b);
        }
    }
}
