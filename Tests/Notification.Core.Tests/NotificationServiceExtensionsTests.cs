using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationServiceExtensionsTests
    {
        [Fact]
        public void Show_WithConfigureAction_DelegatesToService()
        {
            FakeNotificationService service = new FakeNotificationService();

            service.Show(builder => builder
                .WithTitle("Ext Test")
                .WithMessage("Via extension")
                .AsWarning());

            Assert.Single(service.ShownRequests);
            Assert.Equal("Ext Test", service.ShownRequests[0].Title);
            Assert.Equal("Via extension", service.ShownRequests[0].Message);
            Assert.Equal(NotificationType.Warning, service.ShownRequests[0].Type);
        }
    }
}
