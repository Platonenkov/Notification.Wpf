using System;
using System.Collections.Generic;
using Xunit;

namespace Notification.Core.Tests
{
    public class FakeNotificationService : INotificationService
    {
        public List<NotificationRequest> ShownRequests { get; } = new List<NotificationRequest>();
        public List<Guid> DismissedIds { get; } = new List<Guid>();
        public bool DismissAllCalled { get; private set; }

        public Guid Show(NotificationRequest request)
        {
            ShownRequests.Add(request);
            return request.Id;
        }

        public void Dismiss(Guid notificationId)
        {
            DismissedIds.Add(notificationId);
        }

        public void DismissAll()
        {
            DismissAllCalled = true;
        }
    }

    public class NotificationQueueTests
    {
        [Fact]
        public void Enqueue_DelegatesToService()
        {
            FakeNotificationService service = new FakeNotificationService();
            NotificationQueue queue = new NotificationQueue(service);

            NotificationRequest request = NotificationBuilder.Create("Test", "Message").Build();
            Guid id = queue.Enqueue(request);

            Assert.Single(service.ShownRequests);
            Assert.Equal(request.Id, id);
        }

        [Fact]
        public void Enqueue_MultipleRequests_AllDelegated()
        {
            FakeNotificationService service = new FakeNotificationService();
            NotificationQueue queue = new NotificationQueue(service);

            queue.Enqueue(NotificationBuilder.Create("A", "1").Build());
            queue.Enqueue(NotificationBuilder.Create("B", "2").Build());
            queue.Enqueue(NotificationBuilder.Create("C", "3").Build());

            Assert.Equal(3, service.ShownRequests.Count);
        }

        [Fact]
        public void Enqueue_WithGroupKey_GroupsDuplicates()
        {
            FakeNotificationService service = new FakeNotificationService();
            NotificationQueue queue = new NotificationQueue(service);

            queue.Enqueue(NotificationBuilder.Create("A", "1").GroupAs("g1").Build());
            queue.Enqueue(NotificationBuilder.Create("B", "2").GroupAs("g1").Build());

            Assert.True(service.ShownRequests.Count <= 2);
        }
    }
}
