using System;
using System.Threading;
using System.Threading.Tasks;
using Notification.Console;
using Notification.Core;
using Xunit;

namespace Notification.Console.Tests
{
    public class ConsoleNotificationManagerAsyncTests
    {
        [Fact]
        public async Task ShowAsync_completes_immediately_with_timeout()
        {
            ConsoleNotificationManager mgr = new();
            NotificationCloseResult r = await mgr.ShowAsync(new NotificationRequest { Message = "hi" });
            Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
        }

        [Fact]
        public async Task ShowAsync_returns_cancelled_when_token_already_cancelled()
        {
            ConsoleNotificationManager mgr = new();
            NotificationCloseResult r = await mgr.ShowAsync(new NotificationRequest { Message = "hi" }, new CancellationToken(true));
            Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
        }
    }

    public class ConsoleNotificationManagerDialogAsyncTests
    {
        [Fact]
        public async Task ShowDialogAsync_throws_when_buttons_empty()
        {
            ConsoleNotificationManager mgr = new();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                mgr.ShowDialogAsync(new NotificationRequest { Message = "x" }));
        }

        [Fact]
        public async Task ShowDialogAsync_ShowFirstAffirmative_returns_affirmative_button()
        {
            ConsoleNotificationManager mgr = new();
            NotificationRequest req = new()
            {
                Message = "Delete?",
                Buttons = new[]
                {
                    new NotificationButton { Id = "delete", Content = "Delete", Role = NotificationButtonRole.Destructive },
                    new NotificationButton { Id = "yes",    Content = "Yes",    Role = NotificationButtonRole.Affirmative },
                    new NotificationButton { Id = "cancel", Content = "Cancel", Role = NotificationButtonRole.Negative }
                }
            };
            NotificationCloseResult r = await mgr.ShowDialogAsync(req);
            Assert.Equal(NotificationCloseReason.ActionButton, r.Reason);
            Assert.Equal("yes", r.ButtonId);
            Assert.Equal(1, r.ButtonIndex);
        }

        [Fact]
        public async Task ShowDialogAsync_SkipSilently_returns_timeout()
        {
            ConsoleNotificationManager mgr = new();
            NotificationRequest req = new()
            {
                Message = "x",
                Buttons = new[] { new NotificationButton { Id = "ok", Content = "OK" } }
            };
            NotificationCloseResult r = await mgr.ShowDialogAsync(req, NotificationDialogFallback.SkipSilently);
            Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
        }

        [Fact]
        public async Task ShowDialogAsync_Throw_throws_NotSupported()
        {
            ConsoleNotificationManager mgr = new();
            NotificationRequest req = new()
            {
                Message = "x",
                Buttons = new[] { new NotificationButton { Id = "ok", Content = "OK" } }
            };
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                mgr.ShowDialogAsync(req, NotificationDialogFallback.Throw));
        }
    }
}
