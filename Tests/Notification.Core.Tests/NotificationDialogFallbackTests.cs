using Notification.Core;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationDialogFallbackTests
    {
        [Fact]
        public void Enum_has_three_expected_values()
        {
            Assert.Equal(0, (int)NotificationDialogFallback.ShowFirstAffirmative);
            Assert.Equal(1, (int)NotificationDialogFallback.SkipSilently);
            Assert.Equal(2, (int)NotificationDialogFallback.Throw);
        }
    }
}
