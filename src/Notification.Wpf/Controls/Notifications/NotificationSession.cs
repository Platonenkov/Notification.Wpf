using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Notification.Core;
using Notification.Wpf.Utils;

namespace Notification.Wpf.Controls
{
    /// <summary>
    /// Owns one displayed notification and is the only place its close sequence runs. Every close
    /// trigger (expiry, eviction, dismiss, X button, body click, action buttons, progress completion)
    /// ends up in <see cref="CloseAsync"/>, which is idempotent.
    /// </summary>
    internal sealed class NotificationSession
    {
        private const string ToastWindowTitle = "ToastWindow";

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private Task _closeTask;

        internal NotificationSession(Notification control)
        {
            Control = control ?? throw new ArgumentNullException(nameof(control));
            // Cached because CancellationTokenSource.Token throws once the source is disposed (step 7).
            Token = _cancellation.Token;
        }

        /// <summary>Gets the notification control this session owns.</summary>
        internal Notification Control { get; }

        /// <summary>Gets or sets the hosting area; <see langword="null"/> for a notification shown outside an area.</summary>
        internal NotificationArea Area { get; set; }

        /// <summary>Gets a token that is cancelled as soon as the close sequence starts.</summary>
        internal CancellationToken Token { get; }

        /// <summary>Gets a value indicating whether the close sequence has started.</summary>
        internal bool IsClosing => _closeTask != null;

        /// <summary>Gets or sets the reason reported by <see cref="Notification.NotificationClosed"/>; read after the yield, so it can still be set during it.</summary>
        internal NotificationCloseReason? PendingReason { get; set; }

        /// <summary>Gets or sets the action button id when an action button caused the close.</summary>
        internal string PendingButtonId { get; set; }

        /// <summary>Gets or sets the action button index when an action button caused the close.</summary>
        internal int? PendingButtonIndex { get; set; }

        /// <summary>Records <paramref name="reason"/> unless a reason is already pending, then starts the close.</summary>
        internal void RequestClose(NotificationCloseReason? reason = null)
        {
            PendingReason ??= reason;
            // Goes through the control so a throwing NotificationClosed handler surfaces on the
            // dispatcher, as it did before the session existed, instead of vanishing in an unobserved task.
            Control.Close();
        }

        /// <summary>Runs the close sequence once; later calls return the same task.</summary>
        internal Task CloseAsync(bool skipAnimation = false)
        {
            if (_closeTask != null)
                return _closeTask;

            // The task must exist before the first await: a handler of NotificationCloseInvoked may call Close() re-entrantly.
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _closeTask = completion.Task;
            _ = RunCloseAsync(completion, skipAnimation);
            return _closeTask;
        }

        private async Task RunCloseAsync(TaskCompletionSource<bool> completion, bool skipAnimation)
        {
            try
            {
                await CloseCoreAsync(skipAnimation);
                completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }

        private async Task CloseCoreAsync(bool skipAnimation)
        {
            // Stops the timers and polling that observe the token.
            _cancellation.Cancel();

            // Starts the closing animation; bindings stay live.
            Control.RaiseEvent(new RoutedEventArgs(Notification.NotificationCloseInvokedEvent));

            // A dialog button delivers its close reason through Command, which WPF runs AFTER the Click
            // handler that triggered the close. Yield at least once so it has run before the reason is read.
            // Task.Delay yields for a positive animation time; a zero time would complete synchronously,
            // so fall back to an explicit dispatcher yield.
            TimeSpan animationTime = skipAnimation ? TimeSpan.Zero : Control.ClosingAnimationTime;
            if (animationTime > TimeSpan.Zero)
                await Task.Delay(animationTime);
            else
                await Dispatcher.Yield(DispatcherPriority.Background);

            NotificationCloseReason reason = PendingReason ?? NotificationCloseReason.Timeout;

            try
            {
                Control.RaiseEvent(new NotificationClosedRoutedEventArgs(
                    Notification.NotificationClosedEvent, Control, reason, PendingButtonId, PendingButtonIndex));
            }
            finally
            {
                // Removes the control from the area panel and session list and disposes the token source; runs
                // even when a Closed handler throws. Bindings are left alone on purpose: caller-owned elements
                // keep theirs, and template bindings cannot be cleared.
                Area?.ReleaseSession(this);
                _cancellation.Dispose();
            }

            // Last, so the overlay count no longer includes this notification.
            CloseOverlayIfEmpty();
        }

        private void CloseOverlayIfEmpty()
        {
            Window overlay = Area?.OverlayWindow;
            if (overlay == null || overlay.Title != ToastWindowTitle)
                overlay = null;

            if (overlay == null && Application.Current != null)
            {
                try
                {
                    overlay = Application.Current.Windows.OfType<Window>()
                        .FirstOrDefault(x => x.Title.Equals(ToastWindowTitle));
                }
                catch (InvalidOperationException)
                {
                    // The window collection changed while it was enumerated; the next close re-checks.
                }
            }

            if (overlay == null)
                return;

            if (VisualTreeHelperExtensions.GetActiveNotificationCount(overlay) == 0)
                overlay.Close();
        }
    }
}
