using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Notification.Core
{
    public class NotificationQueue : INotificationQueue
    {
        private readonly INotificationService _service;
        private readonly ConcurrentDictionary<string, Guid> _activeGroups = new ConcurrentDictionary<string, Guid>();
        private readonly object _lock = new object();
        private readonly List<(NotificationRequest request, int priority)> _pending = new List<(NotificationRequest, int)>();

        public NotificationQueue(INotificationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

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

        public void RemoveGroup(string groupKey)
        {
            if (groupKey != null)
            {
                _activeGroups.TryRemove(groupKey, out _);
            }
        }
    }
}
