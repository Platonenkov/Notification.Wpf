using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Notification.Core;
using Notification.Wpf.Controls;
using Xunit;
using WpfNotification = Notification.Wpf.Controls.Notification;

namespace Notification.Wpf.Tests
{
    /// <summary>
    /// Behavior coverage for the Close() / button-Command ordering (cold-review finding #1):
    /// clicking the template's action button must execute the button Command (so the wrapper
    /// that records PendingCloseReason runs) and the close must be reported as ActionButton
    /// with ButtonId/ButtonIndex — not as Timeout.
    /// Note: this passes both with and without the early ClearBindingsRecursive that the
    /// review flagged, because the template button's Command binding does not live in the
    /// local value layer and survives ClearAllBindings (verified via physical probe).
    /// </summary>
    // Shares static Areas/_window state with NotificationManagerAsyncTests — same collection
    // keeps them serialized instead of racing on the shared List during enumeration.
    [Collection("NotificationUiState")]
    public class CloseButtonOrderingTests
    {
        private readonly ITestOutputHelper _output;

        public CloseButtonOrderingTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void Action_button_click_executes_command_and_reports_ActionButton()
        {
            StaTask.Run(async () =>
            {
                bool actionExecuted = false;
                var content = new NotificationContent
                {
                    Title = "title",
                    Message = "message",
                    LeftButtonContent = "OK",
                    LeftButtonAction = () => actionExecuted = true
                };

                var area = new NotificationArea();
                var window = new Window
                {
                    Width = 400,
                    Height = 300,
                    Content = area,
                    ShowInTaskbar = false
                };

                try
                {
                    window.Show();

                    var closedTcs = new TaskCompletionSource<NotificationClosedRoutedEventArgs>(
                        TaskCreationOptions.RunContinuationsAsynchronously);

                    WpfNotification? notification = null;
                    area.Show(content, TimeSpan.MaxValue, onClick: null, onClose: null,
                        CloseOnClick: false, ShowXbtn: true,
                        onCreated: n =>
                        {
                            notification = n;
                            n.NotificationClosed += (_, e) =>
                            {
                                if (e is NotificationClosedRoutedEventArgs rea)
                                    closedTcs.TrySetResult(rea);
                            };
                        });

                    // Wait until the area is loaded, the notification is in PART_Items and its
                    // template (containing LeftButton) has been applied.
                    Button? leftButton = null;
                    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                    while (leftButton == null && DateTime.UtcNow < deadline)
                    {
                        if (notification != null)
                            leftButton = FindChildByName<Button>(notification, "LeftButton");
                        if (leftButton == null)
                            await Task.Delay(50);
                    }

                    Assert.NotNull(notification);
                    Assert.NotNull(leftButton);

                    // Diagnostics: this handler subscribes AFTER the template's CloseOnClick
                    // handler, so it runs at the tail of the same RaiseEvent(ClickEvent) call —
                    // i.e. after Close()'s synchronous section but before ExecuteCommandSource.
                    object? commandAtClickRouting = null;
                    bool actionExecutedAtClickRouting = false;
                    bool? isClosingAtRouting = null;
                    leftButton.Click += (_, _) =>
                    {
                        commandAtClickRouting = leftButton.Command;
                        actionExecutedAtClickRouting = actionExecuted;
                        isClosingAtRouting = notification?.IsClosing;
                    };

                    // Drive the click through ButtonBase.OnClick itself — the exact sequence a real
                    // mouse click takes: RaiseEvent(ClickEvent) first, ExecuteCommandSource second.
                    MethodInfo onClickMethod = typeof(ButtonBase)
                        .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.NotNull(onClickMethod);
                    onClickMethod.Invoke(leftButton, null);

                    _output.WriteLine($"[diag] Command inside Click routing: {commandAtClickRouting?.ToString() ?? "<null>"}");
                    _output.WriteLine($"[diag] IsClosing inside Click routing: {isClosingAtRouting}");
                    _output.WriteLine($"[diag] actionExecuted inside Click routing: {actionExecutedAtClickRouting}");
                    _output.WriteLine($"[diag] actionExecuted after OnClick returns: {actionExecuted}");

                    NotificationClosedRoutedEventArgs result =
                        await closedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

                    _output.WriteLine($"[diag] close reason: {result.Reason}, buttonId: {result.ButtonId}, index: {result.ButtonIndex}");

                    Assert.True(actionExecuted,
                        $"LeftButtonAction must run: the button Command must survive until ButtonBase.OnClick executes it. [diag] commandAtClickRouting={commandAtClickRouting?.ToString() ?? "<null>"}, actionAtRouting={actionExecutedAtClickRouting}, actionAfterOnClick={actionExecuted}");
                    Assert.Equal(NotificationCloseReason.ActionButton, result.Reason);
                    Assert.Equal("left", result.ButtonId);
                    Assert.Equal(0, result.ButtonIndex);
                }
                finally
                {
                    window.Close();
                    // Unloaded (→ NotificationManager.RemoveArea) is dispatched asynchronously;
                    // let it flush so the static Areas list is clean for the next test.
                    await Task.Delay(200);
                }
            }, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Public-API variant: ShowDialogAsync with a Buttons entry. Clicking the template's
        /// action button must complete the dialog with Reason=ActionButton and the ButtonId
        /// mapped back to the caller's button id ("ok"), not the internal "left" slot.
        /// Uses a dedicated AreaName so ShowContent never creates/uses the shared overlay window.
        /// </summary>
        [Fact]
        public void ShowDialogAsync_click_reports_ActionButton_with_real_button_id()
        {
            StaTask.Run(async () =>
            {
                var area = new NotificationArea { Name = "StaDialogArea" };
                var window = new Window { Width = 400, Height = 300, Content = area, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    DateTime loadDeadline = DateTime.UtcNow.AddSeconds(5);
                    while (!area.IsLoaded && DateTime.UtcNow < loadDeadline)
                        await Task.Delay(50);
                    Assert.True(area.IsLoaded, "NotificationArea must be loaded (registered in Areas) before ShowDialogAsync");

                    var mgr = new NotificationManager();
                    bool actionExecuted = false;

                    Task<NotificationCloseResult> task = mgr.ShowDialogAsync(new NotificationRequest
                    {
                        AreaName = "StaDialogArea",
                        Title = "title",
                        Message = "message",
                        ExpirationTime = TimeSpan.MaxValue,
                        CloseOnClick = false,
                        LeftButtonAction = () => actionExecuted = true,
                        Buttons = new[]
                        {
                            new NotificationButton { Id = "ok", Content = "OK", Role = NotificationButtonRole.Affirmative }
                        }
                    });

                    Button? leftButton = null;
                    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                    while (leftButton == null && DateTime.UtcNow < deadline)
                    {
                        leftButton = FindChildByName<Button>(window, "LeftButton");
                        if (leftButton == null)
                            await Task.Delay(50);
                    }
                    Assert.NotNull(leftButton);

                    MethodInfo onClickMethod = typeof(ButtonBase)
                        .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.NotNull(onClickMethod);
                    onClickMethod.Invoke(leftButton, null);

                    NotificationCloseResult result = await task.WaitAsync(TimeSpan.FromSeconds(5));
                    _output.WriteLine($"[diag] ShowDialogAsync: reason={result.Reason}, buttonId={result.ButtonId}, index={result.ButtonIndex}, action={actionExecuted}");

                    Assert.True(actionExecuted, "LeftButtonAction must run through the ShowDialogAsync pipeline");
                    Assert.Equal(NotificationCloseReason.ActionButton, result.Reason);
                    Assert.Equal("ok", result.ButtonId);
                    Assert.Equal(0, result.ButtonIndex);
                }
                finally
                {
                    window.Close();
                    // Let the async Unloaded → RemoveArea flush so the static Areas list is clean.
                    await Task.Delay(200);
                }
            }, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Public-API variant: ShowAsync with legacy LeftButtonAction/LeftButtonContent (no
        /// Buttons). Clicking must complete with Reason=ActionButton and the internal slot id
        /// "left" (no caller button id to map back to).
        /// </summary>
        [Fact]
        public void ShowAsync_click_reports_ActionButton_with_legacy_button_id()
        {
            StaTask.Run(async () =>
            {
                var area = new NotificationArea { Name = "StaAsyncArea" };
                var window = new Window { Width = 400, Height = 300, Content = area, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    DateTime loadDeadline = DateTime.UtcNow.AddSeconds(5);
                    while (!area.IsLoaded && DateTime.UtcNow < loadDeadline)
                        await Task.Delay(50);
                    Assert.True(area.IsLoaded, "NotificationArea must be loaded (registered in Areas) before ShowAsync");

                    var mgr = new NotificationManager();
                    bool actionExecuted = false;

                    Task<NotificationCloseResult> task = mgr.ShowAsync(new NotificationRequest
                    {
                        AreaName = "StaAsyncArea",
                        Title = "title",
                        Message = "message",
                        ExpirationTime = TimeSpan.MaxValue,
                        CloseOnClick = false,
                        LeftButtonContent = "OK",
                        LeftButtonAction = () => actionExecuted = true
                    });

                    Button? leftButton = null;
                    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                    while (leftButton == null && DateTime.UtcNow < deadline)
                    {
                        leftButton = FindChildByName<Button>(window, "LeftButton");
                        if (leftButton == null)
                            await Task.Delay(50);
                    }
                    Assert.NotNull(leftButton);

                    MethodInfo onClickMethod = typeof(ButtonBase)
                        .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.NotNull(onClickMethod);
                    onClickMethod.Invoke(leftButton, null);

                    NotificationCloseResult result = await task.WaitAsync(TimeSpan.FromSeconds(5));
                    _output.WriteLine($"[diag] ShowAsync: reason={result.Reason}, buttonId={result.ButtonId}, index={result.ButtonIndex}, action={actionExecuted}");

                    Assert.True(actionExecuted, "LeftButtonAction must run through the ShowAsync pipeline");
                    Assert.Equal(NotificationCloseReason.ActionButton, result.Reason);
                    Assert.Equal("left", result.ButtonId);
                    Assert.Equal(0, result.ButtonIndex);
                }
                finally
                {
                    window.Close();
                    // Let the async Unloaded → RemoveArea flush so the static Areas list is clean.
                    await Task.Delay(200);
                }
            }, TimeSpan.FromSeconds(30));
        }

        private static T? FindChildByName<T>(DependencyObject? root, string name) where T : FrameworkElement
        {
            if (root == null) return null;
            if (root is T typed && typed.Name == name) return typed;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                T? found = FindChildByName<T>(VisualTreeHelper.GetChild(root, i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
