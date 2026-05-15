using System;
using System.Threading;

namespace Notification.Core
{
    public interface INotifierProgress : IProgress<NotificationProgressReport>, IDisposable
    {
        bool IsFinished { get; }
        CancellationToken CancellationToken { get; }
        CancellationTokenSource CancelSource { get; }
        OperationTimer WaitingTimer { get; }
    }
}
