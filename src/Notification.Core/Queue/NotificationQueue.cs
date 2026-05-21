using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Notification.Core
{
    /// <summary>
    /// Manages a queue of notification requests with support for group-based deduplication.
    /// </summary>
    public class NotificationQueue : INotificationQueue
    {
        private readonly INotificationService _service;
        private readonly ConcurrentDictionary<string, Guid> _activeGroups = new ConcurrentDictionary<string, Guid>();
        private readonly object _lock = new object();
        private readonly List<(NotificationRequest request, int priority)> _pending = new List<(NotificationRequest, int)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationQueue"/> class.
        /// </summary>
        /// <param name="service">The notification service used to display notifications.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        public NotificationQueue(INotificationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Enqueues a notification request for display. If a notification with the same group key is already active, returns the existing notification identifier.
        /// </summary>
        /// <param name="request">The notification request to enqueue.</param>
        /// <returns>The unique identifier of the displayed or existing notification.</returns>
        public Guid Enqueue(NotificationRequest request)
        {
            if (request.GroupKey != null && _activeGroups.TryGetValue(request.GroupKey, out Guid existingId))
            {
                return existingId;
            }

            Guid id = _service.Show(request);

            if (request.GroupKey != null)
            {
                _activeGroups[request.GroupKey] = id;
            }

            return id;
        }

        /// <summary>
        /// Gets the number of pending notifications in the queue.
        /// </summary>
        public int PendingCount
        {
            get
            {
                lock (_lock)
                {
                    return _pending.Count;
                }
            }
        }

        /// <summary>
        /// Removes a notification group by its key, allowing new notifications with the same key to be displayed.
        /// </summary>
        /// <param name="groupKey">The group key to remove. If null, no action is taken.</param>
        public void RemoveGroup(string groupKey)
        {
            if (groupKey != null)
            {
                _activeGroups.TryRemove(groupKey, out _);
            }
        }
    }
}
