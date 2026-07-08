using System;
using System.Threading;
using System.Threading.Tasks;
using Notification.Core;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationServiceAsyncExtensionsTests
    {
        private sealed class CapturingService : INotificationService
        {
            public NotificationRequest LastRequest { get; private set; }
            public CancellationToken LastCt { get; private set; }
            public Guid Show(NotificationRequest request) => request.Id;
            public void Dismiss(Guid id) { }
            public void DismissAll() { }
            public Task<NotificationCloseResult> ShowAsync(NotificationRequest req, CancellationToken ct = default)
            {
                LastRequest = req; LastCt = ct;
                return Task.FromResult(new NotificationCloseResult { NotificationId = req.Id, Reason = NotificationCloseReason.Timeout });
            }
            public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest req,
                NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
                CancellationToken ct = default) => throw new NotSupportedException();
        }

        [Fact]
        public async Task Single_argument_overload_builds_message_only_request()
        {
            CapturingService svc = new();
            NotificationCloseResult r = await svc.ShowAsync("hello", NotificationType.Information);

            Assert.NotNull(svc.LastRequest);
            Assert.Null(svc.LastRequest.Title);
            Assert.Equal("hello", svc.LastRequest.Message);
            Assert.Equal(NotificationType.Information, svc.LastRequest.Type);
            Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
        }

        [Fact]
        public async Task Title_message_overload_sets_both()
        {
            CapturingService svc = new();
            await svc.ShowAsync("T", "M", NotificationType.Success);

            Assert.Equal("T", svc.LastRequest.Title);
            Assert.Equal("M", svc.LastRequest.Message);
            Assert.Equal(NotificationType.Success, svc.LastRequest.Type);
        }

        [Fact]
        public async Task Cancellation_token_is_passed_through()
        {
            CapturingService svc = new();
            CancellationTokenSource cts = new();
            await svc.ShowAsync("x", cancellationToken: cts.Token);
            Assert.Equal(cts.Token, svc.LastCt);
        }
    }
}
