using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using Notification.Core;
using Notification.Wpf.Controls;

using Xunit;

using WpfNotification = Notification.Wpf.Controls.Notification;

namespace Notification.Wpf.Tests.Lifecycle
{
    [Collection("Gui")]
    [Trait("Category", "Gui")]
    public class ContentReleaseTests
    {
        private static readonly TimeSpan LongExpiry = TimeSpan.FromHours(1);
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        // Deliberately not INotifyPropertyChanged: a plain object bound through a template is the case
        // where WPF's binding machinery can keep the source and the visual tree alive.
        public sealed class PlainItem
        {
            public string Name { get; set; } = "plain-item";
        }

        private static NotificationRequest CreateRequest(object? content = null, TimeSpan? expiry = null)
        {
            return new NotificationRequest
            {
                Title = "title",
                Message = "message",
                AreaName = "gui",
                Content = content,
                ExpirationTime = expiry ?? LongExpiry
            };
        }

        private static void RegisterPlainItemTemplate(GuiHost host)
        {
            FrameworkElementFactory text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new Binding(nameof(PlainItem.Name)));
            host.Area.Resources[new DataTemplateKey(typeof(PlainItem))] = new DataTemplate(typeof(PlainItem)) { VisualTree = text };
        }

        private static TextBlock? FindTextBlock(DependencyObject root, string text)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is TextBlock block && block.Text == text)
                    return block;

                TextBlock? found = FindTextBlock(child, text);
                if (found != null)
                    return found;
            }

            return null;
        }


        // Observed: the first notification shown in the process stays reachable until a second one is
        // shown, because of WPF-internal one-time state; the library is not the holder. Showing one
        // beforehand makes the measured notification the second one. Each test still asserts its own
        // notification, the warm-up one is never asserted.
        private static async Task WarmUpAsync(GuiHost host)
        {
            RegisterPlainItemTemplate(host);
            await ShowAndCloseAsync(host, "expiry");
            await ShowGeneratedElementAndCloseAsync(host);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<(WeakReference Notification, WeakReference Source)> ShowAndCloseAsync(GuiHost host, string path)
        {
            PlainItem item = new PlainItem();
            NotificationRequest request = CreateRequest(item, path == "expiry" ? TimeSpan.FromMilliseconds(300) : null);
            Task<NotificationCloseResult> result = host.Manager.ShowAsync(request);
            WpfNotification control = host.Area.ActiveSessions[0].Control;
            WeakReference notification = new WeakReference(control);
            WeakReference source = new WeakReference(item);

            await Task.Delay(100);
            Assert.NotNull(FindTextBlock(control, "plain-item"));

            switch (path)
            {
                case "expiry":
                    break;
                case "x":
                    control.ApplyTemplate();
                    Button closeButton = Assert.IsType<Button>(control.Template.FindName("PART_CloseButton", control));
                    closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
                case "body":
                    control.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonDownEvent
                    });
                    break;
                case "dismiss":
                    host.Manager.Dismiss(request.Id);
                    break;
                case "eviction":
                    host.Area.MaxItems = 1;
                    _ = host.Manager.ShowAsync(CreateRequest());
                    break;
            }

            await result.WaitAsync(Wait);
            await Task.Delay(100);
            return (notification, source);
        }

        [Theory]
        [InlineData("expiry")]
        [InlineData("x")]
        [InlineData("body")]
        [InlineData("dismiss")]
        [InlineData("eviction")]
        public void Closed_PlainDataObjectWithTemplate_NotificationIsCollected(string path)
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                RegisterPlainItemTemplate(host);
                await WarmUpAsync(host);

                (WeakReference notification, WeakReference _) = await ShowAndCloseAsync(host, path);

                await GcAssert.CollectedAsync(notification);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<(WeakReference Control, WeakReference Generated)> ShowGeneratedElementAndCloseAsync(GuiHost host)
        {
            Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest());
            WpfNotification control = host.Area.ActiveSessions[0].Control;
            await Task.Delay(100);

            TextBlock generated = FindTextBlock(control, "title") ?? throw new InvalidOperationException("Template did not render the title.");
            WeakReference reference = new WeakReference(generated);
            WeakReference controlReference = new WeakReference(control);
            generated = null!;
            control = null!;

            host.Area.ActiveSessions[0].RequestClose();
            await result.WaitAsync(Wait);
            await Task.Delay(100);
            return (controlReference, reference);
        }

        [Fact]
        public void Closed_GeneratedTreeElement_IsCollected()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();

                await WarmUpAsync(host);
                (WeakReference control, WeakReference generated) = await ShowGeneratedElementAndCloseAsync(host);

                await GcAssert.CollectedAsync(control);
                await GcAssert.CollectedAsync(generated);
            });
        }

        private static TextBlock CreateBoundBlock(PlainItem source)
        {
            TextBlock block = new TextBlock();
            BindingOperations.SetBinding(block, TextBlock.TextProperty, new Binding(nameof(PlainItem.Name)) { Source = source });
            return block;
        }

        private static async Task CloseOnlyAsync(GuiHost host, Task<NotificationCloseResult> result)
        {
            host.Area.ActiveSessions[0].RequestClose();
            await result.WaitAsync(Wait);
            await Task.Delay(100);
        }

        [Fact]
        public void Closed_CallerUIElement_KeepsBindings()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                TextBlock block = CreateBoundBlock(new PlainItem());

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(block));
                await Task.Delay(50);
                await CloseOnlyAsync(host, result);

                Assert.NotNull(BindingOperations.GetBindingExpression(block, TextBlock.TextProperty));
            });
        }

        [Fact]
        public void Closed_CallerUIElement_CanBeShownAgain()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                TextBlock block = CreateBoundBlock(new PlainItem());

                Task<NotificationCloseResult> first = host.Manager.ShowAsync(CreateRequest(block));
                await Task.Delay(50);
                await CloseOnlyAsync(host, first);

                Task<NotificationCloseResult> second = host.Manager.ShowAsync(CreateRequest(block));
                await Task.Delay(100);

                Assert.True(block.IsLoaded);
                Assert.NotNull(BindingOperations.GetBindingExpression(block, TextBlock.TextProperty));
                await CloseOnlyAsync(host, second);
            });
        }

        [Fact]
        public void Closed_CallerIconElement_KeepsBindings()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                TextBlock icon = CreateBoundBlock(new PlainItem());
                NotificationContent content = new NotificationContent { Title = "title", Message = "message", Icon = icon };

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(content));
                await Task.Delay(50);
                await CloseOnlyAsync(host, result);

                Assert.NotNull(BindingOperations.GetBindingExpression(icon, TextBlock.TextProperty));
            });
        }

        [Fact]
        public void Closed_CallerButtonContentElement_KeepsBindings()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                TextBlock buttonContent = CreateBoundBlock(new PlainItem());
                NotificationContent content = new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonContent = buttonContent,
                    LeftButtonAction = () => { }
                };

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(content));
                await Task.Delay(50);
                await CloseOnlyAsync(host, result);

                Assert.NotNull(BindingOperations.GetBindingExpression(buttonContent, TextBlock.TextProperty));
            });
        }

        [Fact]
        public void Closing_ContentCommandStillBound_DuringAnimation()
        {
            GuiThread.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                TextBlock block = CreateBoundBlock(new PlainItem());
                bool boundInHandler = false;

                Task<NotificationCloseResult> result = host.Manager.ShowAsync(CreateRequest(block));
                WpfNotification control = host.Area.ActiveSessions[0].Control;
                await Task.Delay(50);
                control.NotificationCloseInvoked += (_, _) =>
                    boundInHandler = BindingOperations.GetBindingExpression(block, TextBlock.TextProperty) != null;

                host.Area.ActiveSessions[0].RequestClose();
                // Well inside the 400 ms animation, so the content must still be attached.
                await Task.Delay(100);
                Assert.True(boundInHandler);
                Assert.Same(block, control.Content);
                Assert.NotNull(BindingOperations.GetBindingExpression(block, TextBlock.TextProperty));

                await result.WaitAsync(Wait);
            });
        }

        [Fact]
        public void ClosingTime_DefaultTemplate_IsFourHundredMs()
        {
            GuiThread.Run(async () =>
            {
                WpfNotification control = new WpfNotification("x", true);
                control.ApplyTemplate();

                Assert.Equal(TimeSpan.FromMilliseconds(400), WpfNotification.ComputeClosingAnimationTime(control.Template));
                await Task.CompletedTask;
            });
        }
    }

    /// <summary>Builds templates in code and never renders, so it is part of the non-GUI suite.</summary>
    public class ClosingTimeHeadlessTests
    {
        private static ControlTemplate CreateCloseTemplate(Storyboard storyboard)
        {
            EventTrigger trigger = new EventTrigger(WpfNotification.NotificationCloseInvokedEvent);
            trigger.Actions.Add(new BeginStoryboard { Storyboard = storyboard });
            ControlTemplate template = new ControlTemplate(typeof(WpfNotification));
            template.Triggers.Add(trigger);
            return template;
        }

        [Fact]
        public void ClosingTime_AutomaticChild_DoesNotThrow()
        {
            StaTask.Run(async () =>
            {
                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(new DoubleAnimation(0, 1, Duration.Automatic));

                TimeSpan time = WpfNotification.ComputeClosingAnimationTime(CreateCloseTemplate(storyboard));

                Assert.Equal(TimeSpan.Zero, time);
                await Task.CompletedTask;
            });
        }

        [Fact]
        public void ClosingTime_EmptyStoryboard_DoesNotThrow()
        {
            StaTask.Run(async () =>
            {
                TimeSpan time = WpfNotification.ComputeClosingAnimationTime(CreateCloseTemplate(new Storyboard()));

                Assert.Equal(TimeSpan.Zero, time);
                await Task.CompletedTask;
            });
        }
    }
}
