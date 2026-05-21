using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides a queue for scheduling notifications when the maximum display count is reached.
    /// </summary>
    public interface INotificationQueue
    {
        /// <summary>
        /// Adds a notification request to the pending queue.
        /// </summary>
        /// <param name="request">The notification request to enqueue.</param>
        /// <returns>The unique identifier assigned to the queued notification.</returns>
        Guid Enqueue(NotificationRequest request);

        /// <summary>
        /// Gets the number of notifications currently waiting in the queue.
        /// </summary>
        int PendingCount { get; }
    }
}
