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

    public class NotificationButtonClassTests
    {
        [Fact]
        public void Default_role_is_default()
        {
            NotificationButton b = new() { Id = "ok", Content = "OK" };
            Assert.Equal(NotificationButtonRole.Default, b.Role);
        }

        [Fact]
        public void Init_setters_populate_fields()
        {
            NotificationButton b = new()
            {
                Id = "delete",
                Content = "Delete",
                Role = NotificationButtonRole.Destructive
            };
            Assert.Equal("delete", b.Id);
            Assert.Equal("Delete", b.Content);
            Assert.Equal(NotificationButtonRole.Destructive, b.Role);
        }
    }
}
