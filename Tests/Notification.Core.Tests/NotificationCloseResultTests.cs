using Notification.Core;
using Xunit;

namespace Notification.Core.Tests;

public class NotificationCloseReasonTests
{
    [Fact]
    public void Enum_has_expected_five_values()
    {
        Assert.Equal(0, (int)NotificationCloseReason.Timeout);
        Assert.Equal(1, (int)NotificationCloseReason.BodyClick);
        Assert.Equal(2, (int)NotificationCloseReason.XButton);
        Assert.Equal(3, (int)NotificationCloseReason.ActionButton);
        Assert.Equal(4, (int)NotificationCloseReason.Cancelled);
    }
}
