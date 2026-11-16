using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Notification.Core;
using Notification.Wpf.Base;
using Notification.Wpf.Controls;

using Xunit;

using WpfNotification = Notification.Wpf.Controls.Notification;

namespace Notification.Wpf.Tests.Lifecycle
{
    [Collection("Gui")]
    [Trait("Category", "Gui")]
    public class NotificationSessionTests
    {
        private static readonly TimeSpan LongExpiry = TimeSpan.FromHours(1);
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        private static NotificationRequest CreateRequest(TimeSpan? expiry = null, Action? onClose = null)
        {
            return new NotificationRequest
            {
                Title = "title",
                Message = "message",
                AreaName = "gui",
                ExpirationTime = expiry ?? LongExpiry,
                OnClose = onClose
            };
        }

        [Fact]
        public void Close_Twice_RaisesClosedAndOnCloseOnce()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                int onCloseCount = 0;
                int closedCount = 0;

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(onClose: () => onCloseCount++));
                NotificationSession session = host.Area.ActiveSessions[0];
                session.Control.NotificationClosed += (_, _) => closedCount++;

                session.Control.Close();
                session.Control.Close();
                Task first = session.CloseAsync();
                Task second = session.CloseAsync();
                Assert.Same(first, second);

                await result.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Equal(1, closedCount);
                Assert.Equal(1, onCloseCount);
            });
        }

        [Fact]
        public void Close_CancelsSessionToken_BeforeCloseInvokedHandlers()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                bool tokenCancelledInHandler = false;

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest());
                NotificationSession session = host.Area.ActiveSessions[0];
                Assert.False(session.Token.IsCancellationRequested);
                session.Control.NotificationCloseInvoked += (_, _) => tokenCancelledInHandler = session.Token.IsCancellationRequested;

                session.RequestClose();
                await result.WaitAsync(Wait);

                Assert.True(tokenCancelledInHandler);
            });
        }

        [Fact]
        public void PendingReason_SetAfterCloseStarts_IsReported()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest());
                NotificationSession session = host.Area.ActiveSessions[0];

                session.RequestClose();
                session.PendingReason = NotificationCloseReason.ActionButton;

                NotificationCloseResult closed = await result.WaitAsync(Wait);
                Assert.Equal(NotificationCloseReason.ActionButton, closed.Reason);
            });
        }

        [Fact]
        public void Expiry_ReportsTimeout()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();

                NotificationCloseResult closed = await host.Manager
                    .ShowAsync(CreateRequest(expiry: TimeSpan.FromMilliseconds(50)))
                    .WaitAsync(Wait);

                Assert.Equal(NotificationCloseReason.Timeout, closed.Reason);
            });
        }

        [Fact]
        public void XButton_ReportsXButton()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest());
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                control.ApplyTemplate();
                Button closeButton = Assert.IsType<Button>(control.Template.FindName("PART_CloseButton", control));

                closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                NotificationCloseResult closed = await result.WaitAsync(Wait);
                Assert.Equal(NotificationCloseReason.XButton, closed.Reason);
            });
        }

        [Fact]
        public void Dismiss_ReportsCancelled()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                NotificationRequest request = CreateRequest();

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(request);
                host.Manager.Dismiss(request.Id);

                NotificationCloseResult closed = await result.WaitAsync(Wait);
                Assert.Equal(NotificationCloseReason.Cancelled, closed.Reason);
            });
        }

        [Fact]
        public void Eviction_ReportsCancelled()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                host.Area.MaxItems = 1;

                Task<NotificationCloseResult> first = host.Manager.ShowAsync(CreateRequest());
                Task<NotificationCloseResult> second = host.Manager.ShowAsync(CreateRequest());

                NotificationCloseResult closed = await first.WaitAsync(Wait);
                Assert.Equal(NotificationCloseReason.Cancelled, closed.Reason);
                Assert.False(second.IsCompleted);
            });
        }

        [Fact]
        public void CloseFromClosedHandler_DoesNotRepeatOnClose()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                int onCloseCount = 0;
                int closedCount = 0;

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(onClose: () => onCloseCount++));
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                control.NotificationClosed += (_, _) =>
                {
                    closedCount++;
                    control.Close();
                };

                control.Close();
                await result.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Equal(1, closedCount);
                Assert.Equal(1, onCloseCount);
            });
        }

        [Fact]
        public void DismissDuringAnimation_DoesNotRepeatOnClose()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                int onCloseCount = 0;
                int closedCount = 0;
                NotificationRequest request = CreateRequest(onClose: () => onCloseCount++);

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(request);
                NotificationSession session = host.Area.ActiveSessions[0];
                session.Control.NotificationClosed += (_, _) => closedCount++;

                session.Control.Close();
                Assert.True(session.IsClosing);
                host.Manager.Dismiss(request.Id);
                session.RequestClose(NotificationCloseReason.Cancelled);

                await result.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Equal(1, closedCount);
                Assert.Equal(1, onCloseCount);
            });
        }

        [Fact]
        public void CloseInvokedHandlerThrows_TeardownStillCompletes()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                using DispatcherExceptionCapture unhandled = new DispatcherExceptionCapture();

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest());
                NotificationSession session = host.Area.ActiveSessions[0];
                session.Control.NotificationCloseInvoked += (_, _) => throw new InvalidOperationException("close-invoked");

                session.RequestClose();
                await result.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Empty(host.Area.ActiveSessions);
                // The linked progress polling token and the session token source are released with the session.
                Assert.True(session.Token.IsCancellationRequested);
                // The caller's exception is rethrown after the teardown, not swallowed.
                Assert.Contains(unhandled.Exceptions, ex => ex.Message == "close-invoked");
            });
        }

        [Fact]
        public void ClosedHandlerThrows_SessionStillReleased()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                using DispatcherExceptionCapture unhandled = new DispatcherExceptionCapture();

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(onClose: () => throw new InvalidOperationException("on-close")));
                NotificationSession session = host.Area.ActiveSessions[0];

                session.RequestClose();
                await result.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Empty(host.Area.ActiveSessions);
                Assert.Contains(unhandled.Exceptions, ex => ex.Message == "on-close");
            });
        }

        [Fact]
        public void Close_FromOtherThread_IsMarshalledToUiThread()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest());
                NotificationSession session = host.Area.ActiveSessions[0];

                // Without the marshal the close events would be raised on a pool thread and the async void
                // Close would rethrow there, which ends the process.
                await Task.Run(() => session.Control.Close());
                NotificationCloseResult closed = await result.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Equal(NotificationCloseReason.Timeout, closed.Reason);
                Assert.Empty(host.Area.ActiveSessions);
            });
        }

        [Fact]
        public void ShowAsync_AreaCannotDisplay_CompletesAndReleasesBookkeeping()
        {
            GuiThread.Run(async () =>
            {
                // An area that was never loaded is registered but cannot display anything.
                NotificationArea area = new NotificationArea { Name = "CannotDisplay" };
                NotificationManager.AddArea(area);
                try
                {
                    NotificationManager manager = new NotificationManager(System.Windows.Threading.Dispatcher.CurrentDispatcher);
                    Guid id = Guid.NewGuid();
                    NotificationRequest CreateCannotDisplayRequest()
                    {
                        // A fresh request per call: ShowAsync projects the buttons onto the legacy fields of the one it gets.
                        NotificationRequest request = CreateRequest();
                        request.Id = id;
                        request.AreaName = "CannotDisplay";
                        request.Buttons = new[] { new NotificationButton { Id = "ok", Content = "OK" } };
                        return request;
                    }

                    NotificationCloseResult closed = await manager.ShowAsync(CreateCannotDisplayRequest()).WaitAsync(Wait);

                    Assert.Equal(NotificationCloseReason.Timeout, closed.Reason);
                    Assert.Equal(0, CountEntries(manager, "_completions"));
                    Assert.Equal(0, CountEntries(manager, "_buttonsById"));
                    // The same Id can be used again: no "already in flight" entry was left behind.
                    Task<NotificationCloseResult> again = manager.ShowAsync(CreateCannotDisplayRequest());
                    Assert.Equal(NotificationCloseReason.Timeout, (await again.WaitAsync(Wait)).Reason);
                }
                finally
                {
                    NotificationManager.ResetStateForTests();
                }
            });
        }

        private static int CountEntries(NotificationManager manager, string fieldName)
        {
            FieldInfo field = typeof(NotificationManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            return ((ICollection)field.GetValue(manager)!).Count;
        }

        [Fact]
        public void ActiveSessions_IncludeClosingSession_UntilReleased()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest());
                NotificationSession session = host.Area.ActiveSessions[0];

                session.RequestClose();
                Assert.True(session.IsClosing);
                Assert.Contains(session, host.Area.ActiveSessions);

                await result.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Empty(host.Area.ActiveSessions);
            });
        }

        [Fact]
        public void CancelAll_ClosesEverySessionWithReason()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();

                Task<NotificationCloseResult> first = host.Manager.ShowAsync(CreateRequest());
                Task<NotificationCloseResult> second = host.Manager.ShowAsync(CreateRequest());

                host.Area.CancelAll(NotificationCloseReason.Cancelled);

                Assert.Equal(NotificationCloseReason.Cancelled, (await first.WaitAsync(Wait)).Reason);
                Assert.Equal(NotificationCloseReason.Cancelled, (await second.WaitAsync(Wait)).Reason);
            });
        }
    }

    /// <summary>Runs without a visible window, so it is part of the non-GUI suite.</summary>
    public class NotificationSessionHeadlessTests
    {
        private static readonly TimeSpan LongExpiry = TimeSpan.FromHours(1);
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        [Fact]
        public void CannotDisplay_RunsOnCreatedThenClosesWithTimeout()
        {
            StaTask.Run(async () =>
            {
                List<string> order = new List<string>();
                TaskCompletionSource<NotificationCloseReason> closed =
                    new TaskCompletionSource<NotificationCloseReason>(TaskCreationOptions.RunContinuationsAsynchronously);
                int onCloseCount = 0;

                NotificationArea area = new NotificationArea();
                area.Show("x", LongExpiry, null!, () => onCloseCount++, true, true, n =>
                {
                    order.Add("created");
                    n.NotificationClosed += (_, e) =>
                    {
                        order.Add("closed");
                        closed.TrySetResult(((NotificationClosedRoutedEventArgs)e).Reason);
                    };
                });

                NotificationCloseReason reason = await closed.Task.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Equal(new[] { "created", "closed" }, order);
                Assert.Equal(NotificationCloseReason.Timeout, reason);
                Assert.Equal(1, onCloseCount);
            });
        }

        [Fact]
        public void NotificationWithoutArea_ClosesOnce()
        {
            StaTask.Run(async () =>
            {
                int closedCount = 0;
                WpfNotification notification = new WpfNotification("x", true);
                notification.NotificationClosed += (_, _) => closedCount++;

                notification.Close();
                notification.Close();
                await notification.Session.CloseAsync();
                await Task.Delay(100);

                Assert.Equal(1, closedCount);
                Assert.True(notification.IsClosing);
            });
        }
    }
}
