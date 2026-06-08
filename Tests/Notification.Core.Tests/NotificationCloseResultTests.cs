using Notification.Core;
using Xunit;

namespace Notification.Core.Tests
{
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

    public class NotificationCloseResultTests
    {
        [Fact]
        public void IsCancelled_returns_true_only_when_reason_is_cancelled()
        {
            Assert.True(new NotificationCloseResult { Reason = NotificationCloseReason.Cancelled }.IsCancelled);
            Assert.False(new NotificationCloseResult { Reason = NotificationCloseReason.Timeout }.IsCancelled);
            Assert.False(new NotificationCloseResult { Reason = NotificationCloseReason.ActionButton }.IsCancelled);
        }

        [Fact]
        public void Defaults_are_safe()
        {
            NotificationCloseResult r = new();
            Assert.Equal(System.Guid.Empty, r.NotificationId);
            Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
            Assert.Null(r.ButtonId);
            Assert.Null(r.ButtonIndex);
            Assert.False(r.IsCancelled);
        }

        [Fact]
        public void Init_setters_populate_fields()
        {
            System.Guid id = System.Guid.NewGuid();
            NotificationCloseResult r = new()
            {
                NotificationId = id,
                Reason = NotificationCloseReason.ActionButton,
                ButtonId = "ok",
                ButtonIndex = 0
            };
            Assert.Equal(id, r.NotificationId);
            Assert.Equal(NotificationCloseReason.ActionButton, r.Reason);
            Assert.Equal("ok", r.ButtonId);
            Assert.Equal(0, r.ButtonIndex);
        }
    }
}
