using System;
using System.Threading;
using System.Threading.Tasks;
using Notification.Core;
using Notification.Core.Internal;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationCompletionSourceTests
    {
        private sealed class FakeService : INotificationService
        {
            public int DismissCallCount { get; private set; }
            public Guid DismissedId { get; private set; }
            public Guid Show(NotificationRequest request) => Guid.Empty;
            public void Dismiss(Guid id) { DismissCallCount++; DismissedId = id; }
            public void DismissAll() { }
            public Task<NotificationCloseResult> ShowAsync(NotificationRequest req, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest req,
                NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
                CancellationToken ct = default) => throw new NotSupportedException();
        }

        [Fact]
        public async Task Complete_sets_reason_button_and_id()
        {
            Guid id = Guid.NewGuid();
            FakeService svc = new();
            NotificationCompletionSource src = new(id, svc, CancellationToken.None);

            src.Complete(NotificationCloseReason.ActionButton, "ok", 0);

            NotificationCloseResult r = await src.Task;
            Assert.Equal(id, r.NotificationId);
            Assert.Equal(NotificationCloseReason.ActionButton, r.Reason);
            Assert.Equal("ok", r.ButtonId);
            Assert.Equal(0, r.ButtonIndex);
        }

        [Fact]
        public async Task Cancellation_completes_with_cancelled_and_calls_dismiss()
        {
            Guid id = Guid.NewGuid();
            FakeService svc = new();
            CancellationTokenSource cts = new();
            NotificationCompletionSource src = new(id, svc, cts.Token);

            cts.Cancel();

            NotificationCloseResult r = await src.Task;
            Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
            Assert.Equal(1, svc.DismissCallCount);
            Assert.Equal(id, svc.DismissedId);
        }

        [Fact]
        public async Task Already_cancelled_token_completes_immediately()
        {
            Guid id = Guid.NewGuid();
            FakeService svc = new();
            CancellationToken alreadyCancelled = new CancellationToken(canceled: true);
            NotificationCompletionSource src = new(id, svc, alreadyCancelled);

            NotificationCloseResult r = await src.Task;
            Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
        }

        [Fact]
        public async Task Multiple_complete_calls_keep_first_result()
        {
            Guid id = Guid.NewGuid();
            FakeService svc = new();
            NotificationCompletionSource src = new(id, svc, CancellationToken.None);

            src.Complete(NotificationCloseReason.Timeout);
            src.Complete(NotificationCloseReason.BodyClick);

            NotificationCloseResult r = await src.Task;
            Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
        }
    }
}
