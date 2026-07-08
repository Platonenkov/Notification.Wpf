using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notification.Core.Internal
{
    /// <summary>
    /// Bridges a notification's lifecycle to a <see cref="TaskCompletionSource{TResult}"/>
    /// that resolves with a <see cref="NotificationCloseResult"/>. Registers cancellation
    /// to invoke <see cref="INotificationService.Dismiss"/> and complete with
    /// <see cref="NotificationCloseReason.Cancelled"/>.
    /// </summary>
    internal sealed class NotificationCompletionSource
    {
        private readonly TaskCompletionSource<NotificationCloseResult> _tcs =
            new TaskCompletionSource<NotificationCloseResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly Guid _id;
        private CancellationTokenRegistration _ctReg;

        public NotificationCompletionSource(Guid id, INotificationService service, CancellationToken ct)
        {
            _id = id;
            if (ct.CanBeCanceled)
            {
                _ctReg = ct.Register(() =>
                {
                    try { service.Dismiss(_id); } catch { }
                    _tcs.TrySetResult(new NotificationCloseResult
                    {
                        NotificationId = _id,
                        Reason = NotificationCloseReason.Cancelled
                    });
                });
            }
        }

        public Task<NotificationCloseResult> Task => _tcs.Task;

        public void Complete(NotificationCloseReason reason, string buttonId = null, int? buttonIndex = null)
        {
            _ctReg.Dispose();
            _tcs.TrySetResult(new NotificationCloseResult
            {
                NotificationId = _id,
                Reason = reason,
                ButtonId = buttonId,
                ButtonIndex = buttonIndex
            });
        }
    }
}
