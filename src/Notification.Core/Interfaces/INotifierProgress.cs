using System;
using System.Threading;

namespace Notification.Core
{
    /// <summary>
    /// Represents a progress reporter for notification-based operations with cancellation and timing support.
    /// </summary>
    public interface INotifierProgress : IProgress<NotificationProgressReport>, IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the associated operation has finished.
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Gets the cancellation token that signals when the operation should be cancelled.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the cancellation token source used to trigger cancellation of the operation.
        /// </summary>
        CancellationTokenSource CancelSource { get; }

        /// <summary>
        /// Gets the timer that tracks the elapsed waiting time of the operation.
        /// </summary>
        OperationTimer WaitingTimer { get; }
    }
}
