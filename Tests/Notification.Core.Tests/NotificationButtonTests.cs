using Notification.Core;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationButtonRoleTests
    {
        [Fact]
        public void Enum_has_four_expected_values()
        {
            Assert.Equal(0, (int)NotificationButtonRole.Default);
            Assert.Equal(1, (int)NotificationButtonRole.Affirmative);
            Assert.Equal(2, (int)NotificationButtonRole.Negative);
            Assert.Equal(3, (int)NotificationButtonRole.Destructive);
        }
    }
}
