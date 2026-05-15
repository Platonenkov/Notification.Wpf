using System;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationEventServiceTests
    {
        [Fact]
        public void Raise_Shown_FiresLifecycleChangedAndShown()
        {
            NotificationEventService service = new NotificationEventService();
            bool lifecycleChanged = false;
            bool shownFired = false;

            service.NotificationLifecycleChanged += (s, e) => lifecycleChanged = true;
            service.NotificationShown += (s, e) => shownFired = true;

            service.Raise(new NotificationLifecycleEventArgs(
                Guid.NewGuid(), NotificationLifecycleStage.Shown, "Test", "Message"));

            Assert.True(lifecycleChanged);
            Assert.True(shownFired);
        }

        [Fact]
        public void Raise_Closed_FiresLifecycleChangedAndClosed()
        {
            NotificationEventService service = new NotificationEventService();
            bool closedFired = false;

            service.NotificationClosed += (s, e) => closedFired = true;

            service.Raise(new NotificationLifecycleEventArgs(
                Guid.NewGuid(), NotificationLifecycleStage.Closed, "Test", "Message"));

            Assert.True(closedFired);
        }

        [Fact]
        public void Raise_Clicked_FiresLifecycleChangedAndClicked()
        {
            NotificationEventService service = new NotificationEventService();
            bool clickedFired = false;

            service.NotificationClicked += (s, e) => clickedFired = true;

            service.Raise(new NotificationLifecycleEventArgs(
                Guid.NewGuid(), NotificationLifecycleStage.Clicked, "Test", "Message"));

            Assert.True(clickedFired);
        }

        [Fact]
        public void Raise_Dismissed_FiresLifecycleChangedAndClosed()
        {
            NotificationEventService service = new NotificationEventService();
            bool closedFired = false;

            service.NotificationClosed += (s, e) => closedFired = true;

            service.Raise(new NotificationLifecycleEventArgs(
                Guid.NewGuid(), NotificationLifecycleStage.Dismissed, null, null));

            Assert.True(closedFired);
        }

        [Fact]
        public void Raise_TimedOut_FiresLifecycleChangedAndClosed()
        {
            NotificationEventService service = new NotificationEventService();
            bool closedFired = false;

            service.NotificationClosed += (s, e) => closedFired = true;

            service.Raise(new NotificationLifecycleEventArgs(
                Guid.NewGuid(), NotificationLifecycleStage.TimedOut, null, null));

            Assert.True(closedFired);
        }

        [Fact]
        public void Raise_PreservesEventArgsProperties()
        {
            NotificationEventService service = new NotificationEventService();
            Guid id = Guid.NewGuid();
            NotificationLifecycleEventArgs? captured = null;

            service.NotificationLifecycleChanged += (s, e) => captured = e;

            service.Raise(new NotificationLifecycleEventArgs(
                id, NotificationLifecycleStage.Shown, "Title", "Msg"));

            Assert.NotNull(captured);
            Assert.Equal(id, captured!.NotificationId);
            Assert.Equal(NotificationLifecycleStage.Shown, captured.Stage);
            Assert.Equal("Title", captured.Title);
            Assert.Equal("Msg", captured.Message);
            Assert.True(captured.Timestamp <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void Raise_WithNoSubscribers_DoesNotThrow()
        {
            NotificationEventService service = new NotificationEventService();

            Exception? ex = Record.Exception(() =>
                service.Raise(new NotificationLifecycleEventArgs(
                    Guid.NewGuid(), NotificationLifecycleStage.Shown, null, null)));

            Assert.Null(ex);
        }
    }
}
