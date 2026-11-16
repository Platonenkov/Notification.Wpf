using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

using Notification.Core;
using Notification.Wpf.Base;
using Notification.Wpf.Classes;
using Notification.Wpf.Controls;
using Notifications.Wpf.View;
using Notifications.Wpf.ViewModels;

using Xunit;

using WpfNotification = Notification.Wpf.Controls.Notification;

namespace Notification.Wpf.Tests.Lifecycle
{
    [Collection("Gui")]
    [Trait("Category", "Gui")]
    public class NotificationViewModelTests
    {
        private static readonly TimeSpan LongExpiry = TimeSpan.FromHours(1);
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        private static NotificationRequest CreateRequest(NotificationContent content)
        {
            return new NotificationRequest { AreaName = "gui", Content = content, ExpirationTime = LongExpiry };
        }

        private static async Task WaitUntilEmptyAsync(NotificationArea area)
        {
            DateTime deadline = DateTime.UtcNow + Wait;
            while (area.ActiveSessions.Count > 0 && DateTime.UtcNow < deadline)
                await Task.Delay(20);

            Assert.Empty(area.ActiveSessions);
            await Task.Delay(100);
        }

        private static Button? FindButton(DependencyObject root, string name)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is Button button && button.Name == name)
                    return button;

                Button? found = FindButton(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        // A real click: the automation peer goes through ButtonBase.OnClick, so Click and then Command run
        // exactly as for a mouse click. It is queued on the dispatcher, so callers await afterwards.
        private static void Click(WpfNotification control, string buttonName)
        {
            Button button = FindButton(control, buttonName) ?? throw new InvalidOperationException(buttonName + " was not rendered.");
            IInvokeProvider invoke = (IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke);
            invoke.Invoke();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<WeakReference> ShowAndCloseAsync(GuiHost host, NotificationContent content)
        {
            host.Manager.Show(content, "gui", LongExpiry);
            WpfNotification control = host.Area.ActiveSessions[0].Control;
            WeakReference reference = new WeakReference(control);
            await Task.Delay(100);

            host.Area.ActiveSessions[0].RequestClose();
            await WaitUntilEmptyAsync(host.Area);
            return reference;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<WeakReference> ShowAndClickLeftAsync(GuiHost host)
        {
            Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(new NotificationContent
            {
                Title = "title",
                Message = "message",
                LeftButtonAction = () => { }
            }));
            WpfNotification control = host.Area.ActiveSessions[0].Control;
            WeakReference reference = new WeakReference(control);
            await Task.Delay(100);

            Click(control, "LeftButton");
            await result.WaitAsync(Wait);
            await Task.Delay(100);
            return reference;
        }

        // The first notifications shown in a process stay reachable through WPF-internal one-time
        // state; see ContentReleaseTests. Each test still asserts only its own notification.
        private static async Task WarmUpAsync(GuiHost host)
        {
            await ShowAndCloseAsync(host, new NotificationContent { Title = "warm", Message = "up" });
            await ShowAndCloseAsync(host, new NotificationContent { Title = "warm", Message = "up" });
        }

        [Fact]
        public void LeftButton_Click_RunsActionAndReportsActionButton()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                int runs = 0;
                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonAction = () => runs++
                }));
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                await Task.Delay(100);

                Click(control, "LeftButton");
                NotificationCloseResult closed = await result.WaitAsync(Wait);

                Assert.Equal(1, runs);
                Assert.Equal(NotificationCloseReason.ActionButton, closed.Reason);
                Assert.Equal("left", closed.ButtonId);
                Assert.Equal(0, closed.ButtonIndex);
            });
        }

        [Fact]
        public void RightButton_Click_RunsActionAndReportsActionButton()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                int runs = 0;
                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    RightButtonAction = () => runs++
                }));
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                await Task.Delay(100);

                Click(control, "RightButton");
                NotificationCloseResult closed = await result.WaitAsync(Wait);

                Assert.Equal(1, runs);
                Assert.Equal(NotificationCloseReason.ActionButton, closed.Reason);
                Assert.Equal("right", closed.ButtonId);
                Assert.Equal(1, closed.ButtonIndex);
            });
        }

        [Fact]
        public void ShowDialogAsync_ButtonClick_ReturnsRealButtonId()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                Task<NotificationCloseResult> result = host.Manager.ShowDialogAsync(new NotificationRequest
                {
                    Title = "title",
                    Message = "message",
                    AreaName = "gui",
                    Buttons = new[]
                    {
                        new NotificationButton { Id = "yes", Content = "Yes", Role = NotificationButtonRole.Affirmative },
                        new NotificationButton { Id = "no", Content = "No", Role = NotificationButtonRole.Negative }
                    }
                });
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                await Task.Delay(100);

                Click(control, "LeftButton");
                NotificationCloseResult closed = await result.WaitAsync(Wait);

                Assert.Equal(NotificationCloseReason.ActionButton, closed.Reason);
                Assert.Equal("yes", closed.ButtonId);
                Assert.Equal(0, closed.ButtonIndex);
            });
        }

        [Fact]
        public void DoubleClick_RunsActionOnce()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                int runs = 0;
                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonAction = () => runs++
                }));
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                await Task.Delay(100);

                Click(control, "LeftButton");
                Click(control, "LeftButton");
                await result.WaitAsync(Wait);
                await Task.Delay(100);

                Assert.Equal(1, runs);
            });
        }

        [Fact]
        public void ThrowingAction_StillCloses()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonAction = () => throw new InvalidOperationException("boom")
                }));
                NotificationSession session = host.Area.ActiveSessions[0];
                NotificationViewModel model = Assert.IsType<NotificationViewModel>(session.Control.Content);

                Assert.Throws<InvalidOperationException>(() => model.LeftButtonCommand.Execute(null));

                Assert.True(session.IsClosing);
                NotificationCloseResult closed = await result.WaitAsync(Wait);
                Assert.Equal(NotificationCloseReason.ActionButton, closed.Reason);
            });
        }

        [Fact]
        public void Show_DoesNotMutateCallerContent()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                Action left = () => { };
                Action right = () => { };
                NotificationContent content = new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonAction = left,
                    RightButtonAction = right
                };

                host.Manager.Show(content, "gui", LongExpiry);
                await Task.Delay(100);

                Assert.Same(left, content.LeftButtonAction);
                Assert.Same(right, content.RightButtonAction);
                host.Area.ActiveSessions[0].RequestClose();
                await WaitUntilEmptyAsync(host.Area);
            });
        }

        [Fact]
        public void SameContentShownTwice_FirstNotificationIsCollected()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                await WarmUpAsync(host);

                NotificationContent content = new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonAction = () => { }
                };

                WeakReference first = await ShowAndCloseAsync(host, content);
                host.Manager.Show(content, "gui", LongExpiry);
                await Task.Delay(100);

                await GcAssert.CollectedAsync(first);
                host.Area.ActiveSessions[0].RequestClose();
                await WaitUntilEmptyAsync(host.Area);
            });
        }

        [Fact]
        public void SameContentInTwoAreasWithOneName_ButtonsAreIndependent()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost first = await GuiHost.CreateAsync();
                using GuiHost second = await GuiHost.CreateAsync();
                int runs = 0;
                NotificationContent content = new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonAction = () => runs++
                };

                first.Manager.Show(content, "gui", LongExpiry);
                NotificationSession firstSession = first.Area.ActiveSessions[0];
                NotificationSession secondSession = second.Area.ActiveSessions[0];
                await Task.Delay(100);

                Assert.NotSame(firstSession.Control.Content, secondSession.Control.Content);

                Click(firstSession.Control, "LeftButton");
                await WaitUntilEmptyAsync(first.Area);

                Assert.Equal(1, runs);
                Assert.False(secondSession.IsClosing);

                Click(secondSession.Control, "LeftButton");
                await WaitUntilEmptyAsync(second.Area);

                Assert.Equal(2, runs);
            });
        }

        [Fact]
        public void Closed_ViaActionButton_IsCollected()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                await WarmUpAsync(host);

                WeakReference control = await ShowAndClickLeftAsync(host);

                await GcAssert.CollectedAsync(control);
            });
        }

        [Fact]
        public void AttachButton_OpensContentWindow()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                host.Manager.Show(new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    TrimType = NotificationTextTrimType.Attach
                }, "gui", LongExpiry);
                NotificationSession session = host.Area.ActiveSessions[0];
                NotificationViewModel model = Assert.IsType<NotificationViewModel>(session.Control.Content);
                await Task.Delay(100);

                Click(session.Control, "Attach");
                await Task.Delay(200);

                Window? opened = null;
                foreach (PresentationSource source in PresentationSource.CurrentSources)
                {
                    if (source.RootVisual is Window window && window.Content is TextContentView view && ReferenceEquals(view.DataContext, model))
                        opened = window;
                }

                Assert.NotNull(opened);
                opened!.Close();
                await WaitUntilEmptyAsync(host.Area);
            });
        }

        [Fact]
        public void CustomTemplate_CloseOnClick_StillRecordsCommandReason()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                int runs = 0;
                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonAction = () => runs++
                }));
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                // A template written for older versions: CloseOnClick closes in Click, before Command runs.
                control.ContentTemplate = (DataTemplate)XamlReader.Parse(
                    "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
                    "xmlns:c=\"clr-namespace:Notification.Wpf.Controls;assembly=Notification.Wpf\">" +
                    "<Button x:Name=\"Custom\" Content=\"go\" Command=\"{Binding LeftButtonCommand}\" c:Notification.CloseOnClick=\"True\"/>" +
                    "</DataTemplate>");
                await Task.Delay(100);

                Click(control, "Custom");
                NotificationCloseResult closed = await result.WaitAsync(Wait);

                Assert.Equal(1, runs);
                Assert.Equal(NotificationCloseReason.ActionButton, closed.Reason);
                Assert.Equal("left", closed.ButtonId);
            });
        }
    }

    /// <summary>
    /// Builds view models without showing anything, so it stays in the non-GUI suite. It still runs on the
    /// shared GUI thread because the default content values come from thread-affine statics.
    /// </summary>
    [Collection("Gui")]
    public class NotificationViewModelHeadlessTests
    {
        public sealed class ExtendedContent : NotificationContent
        {
            public string Extra { get; set; } = "extra";
        }

        private static NotificationViewModel CreateModel(NotificationContent content)
        {
            return new NotificationViewModel(content, new WpfNotification().Session);
        }

        [Fact]
        public void ViewModel_CopiesAllBoundProperties()
        {
            GuiThread.Run(async () =>
            {
                NotificationContent content = new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    Type = NotificationType.Warning,
                    Image = new NotificationImage { Position = ImagePosition.Top },
                    Icon = new object(),
                    IconForeground = new SolidColorBrush(Colors.Red),
                    Background = new SolidColorBrush(Colors.Green),
                    Foreground = new SolidColorBrush(Colors.Blue),
                    TitleTextSettings = new TextContentSettings(),
                    MessageTextSettings = new TextContentSettings(),
                    TrimType = NotificationTextTrimType.Trim,
                    RowsCount = 7,
                    LeftButtonContent = "L",
                    RightButtonContent = "R"
                };

                NotificationViewModel model = CreateModel(content);

                Assert.Same(content, model.Source);
                Assert.Equal(content.Title, model.Title);
                Assert.Equal(content.Message, model.Message);
                Assert.Equal(content.Type, model.Type);
                Assert.Same(content.Image, model.Image);
                Assert.Same(content.Icon, model.Icon);
                Assert.Same(content.IconForeground, model.IconForeground);
                Assert.Same(content.Background, model.Background);
                Assert.Same(content.Foreground, model.Foreground);
                Assert.Same(content.TitleTextSettings, model.TitleTextSettings);
                Assert.Same(content.MessageTextSettings, model.MessageTextSettings);
                Assert.Equal(content.TrimType, model.TrimType);
                Assert.Equal(7u, model.RowsCount);
                Assert.Equal("L", model.LeftButtonContent);
                Assert.Equal("R", model.RightButtonContent);
                Assert.Null(model.LeftButtonCommand);
                Assert.Null(model.RightButtonCommand);
                await Task.CompletedTask;
            });
        }

        [Fact]
        public void Subclass_IsReachableThroughSource()
        {
            GuiThread.Run(async () =>
            {
                ExtendedContent content = new ExtendedContent { Title = "title", Extra = "payload" };

                NotificationViewModel model = CreateModel(content);

                Assert.Same(content, model.Source);
                Assert.Equal("payload", Assert.IsType<ExtendedContent>(model.Source).Extra);
                await Task.CompletedTask;
            });
        }

        [Fact]
        public void AttachIfMoreRows_ComputesVisibility()
        {
            GuiThread.Run(async () =>
            {
                NotificationAttachVisibleConverter converter = new NotificationAttachVisibleConverter();
                NotificationViewModel shortMessage = CreateModel(new NotificationContent { Message = "short", RowsCount = 1 });
                NotificationViewModel longMessage = CreateModel(new NotificationContent { Message = new string('x', 100), RowsCount = 1 });

                Assert.Equal(Visibility.Collapsed, converter.Convert(shortMessage, typeof(Visibility), null!, CultureInfo.InvariantCulture));
                Assert.Equal(Visibility.Visible, converter.Convert(longMessage, typeof(Visibility), null!, CultureInfo.InvariantCulture));
                Assert.Equal(Visibility.Collapsed, converter.Convert(new object(), typeof(Visibility), null!, CultureInfo.InvariantCulture));
                await Task.CompletedTask;
            });
        }
    }
}
