using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Notification.Avalonia.Controls;
using Notification.Avalonia.Progress;
using Notification.Core;
using Notification.Core.Internal;

namespace Notification.Avalonia
{
    /// <summary>
    /// Avalonia notification manager with support for toast and progress notifications.
    /// Supports two display modes: in-window overlay and separate topmost overlay window.
    /// </summary>
    public class AvaloniaNotificationManager : INotificationService
    {
        private readonly INotificationConfiguration _config;
        private readonly INotificationEventService _events;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeNotifications =
            new ConcurrentDictionary<Guid, CancellationTokenSource>();

        private readonly ConcurrentDictionary<Guid, NotificationCompletionSource> _completions = new();
        private readonly ConcurrentDictionary<Guid, IReadOnlyList<NotificationButton>> _buttonsById = new();

        private AvaloniaNotificationHost _host;
        private Window _parentWindow;
        private NotificationOverlayWindow _overlayWindow;
        private bool _useOverlayWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaNotificationManager"/> class.
        /// </summary>
        public AvaloniaNotificationManager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaNotificationManager"/> class
        /// with the specified configuration and event service.
        /// </summary>
        /// <param name="config">The notification configuration.</param>
        /// <param name="events">The event service used to raise lifecycle events.</param>
        public AvaloniaNotificationManager(INotificationConfiguration config, INotificationEventService events)
        {
            _config = config;
            _events = events;
        }

        /// <summary>
        /// Attach the notification manager to a TopLevel (Window or similar).
        /// Must be called before showing notifications.
        /// </summary>
        public void SetHost(TopLevel host)
        {
            _parentWindow = host as Window;

            _host = new AvaloniaNotificationHost(host)
            {
                MaxItems = (int)(_config?.MaxOverlayWindowCount ?? 5),
                Position = _config?.MessagePosition ?? NotificationPosition.BottomRight
            };
        }

        /// <summary>
        /// Gets or sets whether to use a separate overlay window (true)
        /// or display notifications inside the host window (false).
        /// Can be toggled at runtime. Existing notifications are dismissed on switch.
        /// </summary>
        public bool UseOverlayWindow
        {
            get => _useOverlayWindow;
            set
            {
                if (_useOverlayWindow == value)
                    return;

                // Dismiss all from current host before switching
                ActiveHost?.CloseAll();

                _useOverlayWindow = value;

                if (value)
                {
                    EnsureOverlayWindow();
                }
                else
                {
                    CloseOverlayWindow();
                }
            }
        }

        /// <summary>
        /// Gets the currently active notification host (in-window or overlay).
        /// </summary>
        private AvaloniaNotificationHost ActiveHost
        {
            get
            {
                if (_useOverlayWindow)
                {
                    EnsureOverlayWindow();
                    return _overlayWindow?.Host;
                }
                return _host;
            }
        }

        /// <summary>
        /// Gets or sets the notification position. Can be changed at runtime.
        /// </summary>
        public NotificationPosition Position
        {
            get
            {
                AvaloniaNotificationHost host = ActiveHost;
                return host?.Position ?? NotificationPosition.BottomRight;
            }
            set
            {
                // Apply to both hosts so position is preserved when switching modes
                if (_host != null)
                    _host.Position = value;
                if (_overlayWindow?.Host != null)
                    _overlayWindow.Host.Position = value;

                // Reposition overlay window when position changes
                RepositionOverlayWindow(value);
            }
        }

        /// <summary>
        /// Show a notification with custom Avalonia content.
        /// </summary>
        /// <param name="content">Custom Avalonia control to display</param>
        /// <param name="expirationTime">Time before auto-dismiss (null uses default)</param>
        /// <param name="backgroundColor">Optional background color</param>
        /// <returns>Notification ID</returns>
        public Guid ShowCustomContent(Control content, TimeSpan? expirationTime = null,
            NotificationColor? backgroundColor = null)
        {
            AvaloniaNotificationHost host = ActiveHost;
            if (host == null)
            {
                throw new InvalidOperationException(
                    "No host set. Call SetHost() before showing notifications.");
            }

            TimeSpan expiration = expirationTime ?? _config?.DefaultExpirationTime ?? TimeSpan.FromSeconds(5);

            TimeSpan maxSafe = TimeSpan.FromMilliseconds(int.MaxValue);
            if (expiration > maxSafe)
                expiration = maxSafe;

            Guid id = host.ShowCustomContent(content, expiration, backgroundColor);

            _events?.Raise(new NotificationLifecycleEventArgs(
                id, NotificationLifecycleStage.Shown, "Custom Content", null));

            return id;
        }

        /// <summary>
        /// Show a toast notification from a NotificationRequest.
        /// </summary>
        public Guid Show(NotificationRequest request)
        {
            Guid id = request.Id;

            AvaloniaNotificationHost host = ActiveHost;
            if (host == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Notification.Avalonia] No host set. Call SetHost() first. Notification: {request.Title}");
                return id;
            }

            TimeSpan expiration = request.ExpirationTime ?? _config?.DefaultExpirationTime ?? TimeSpan.FromSeconds(5);

            // Clamp to safe max for timers
            TimeSpan maxSafe = TimeSpan.FromMilliseconds(int.MaxValue);
            if (expiration > maxSafe)
                expiration = maxSafe;

            host.ShowToast(request, expiration);

            _events?.Raise(new NotificationLifecycleEventArgs(
                id, NotificationLifecycleStage.Shown, request.Title, request.Message));

            return id;
        }

        /// <summary>
        /// Show a progress bar notification. Returns INotifierProgress to update progress.
        /// Dispose the returned object to close the notification.
        /// </summary>
        /// <param name="title">Initial title text</param>
        /// <param name="showCancelButton">Whether to show a Cancel button</param>
        /// <param name="waitingMessage">Base message for time estimation (null to disable)</param>
        /// <returns>Progress handle implementing INotifierProgress</returns>
        public INotifierProgress ShowProgressBar(
            string title = "Processing...",
            bool showCancelButton = false,
            string waitingMessage = null)
        {
            AvaloniaNotificationHost host = ActiveHost;
            if (host == null)
            {
                throw new InvalidOperationException(
                    "No host set. Call SetHost() before showing progress notifications.");
            }

            ProgressCardHandle handle = host.ShowProgress(title, showCancelButton);

            AvaloniaNotifierProgress progress = new AvaloniaNotifierProgress(handle);

            if (waitingMessage != null)
                progress.WaitingTimer.BaseWaitingMessage = waitingMessage;

            _events?.Raise(new NotificationLifecycleEventArgs(
                handle.Id, NotificationLifecycleStage.Shown, title, null));

            // Poll for completion to fire lifecycle event
            PollForCompletion(handle.Id, progress, title);

            return progress;
        }

        private async void PollForCompletion(Guid id, AvaloniaNotifierProgress progress, string title)
        {
            try
            {
                while (!progress.IsFinished)
                {
                    await Task.Delay(500);
                }

                _events?.Raise(new NotificationLifecycleEventArgs(
                    id, NotificationLifecycleStage.Closed, title, null));
            }
            catch
            {
                // Ignored
            }
        }

        /// <summary>
        /// Dismiss a notification by ID.
        /// </summary>
        public void Dismiss(Guid notificationId)
        {
            ActiveHost?.Close(notificationId);
            _events?.Raise(new NotificationLifecycleEventArgs(
                notificationId, NotificationLifecycleStage.Dismissed, null, null));
        }

        /// <summary>
        /// Dismiss all active notifications.
        /// </summary>
        public void DismissAll()
        {
            ActiveHost?.CloseAll();
        }

        private void EnsureOverlayWindow()
        {
            if (_overlayWindow != null && _overlayWindow.IsVisible)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                if (_overlayWindow != null)
                    return;

                NotificationPosition position = _host?.Position ?? _config?.MessagePosition ?? NotificationPosition.BottomRight;

                _overlayWindow = new NotificationOverlayWindow();
                _overlayWindow.Host.MaxItems = (int)(_config?.MaxOverlayWindowCount ?? 5);
                _overlayWindow.Host.Position = position;

                _overlayWindow.Closed += (s, e) =>
                {
                    _overlayWindow = null;
                };

                if (_parentWindow != null)
                {
                    _overlayWindow.ApplyPositionOnScreen(_parentWindow, position);
                    _overlayWindow.BindToParent(_parentWindow);
                }

                _overlayWindow.Show();
            });
        }

        private void RepositionOverlayWindow(NotificationPosition position)
        {
            if (_overlayWindow == null || _parentWindow == null)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                _overlayWindow?.ApplyPositionOnScreen(_parentWindow, position);
            });
        }

        private void CloseOverlayWindow()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_overlayWindow != null)
                {
                    _overlayWindow.Close();
                    _overlayWindow = null;
                }
            });
        }

        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

#pragma warning disable CS0618
            bool hasLegacy = !string.IsNullOrEmpty(request.LeftButtonContent) || !string.IsNullOrEmpty(request.RightButtonContent);
#pragma warning restore CS0618
            if (request.Buttons is { Count: > 0 } && hasLegacy)
                throw new ArgumentException(
                    "NotificationRequest specifies both Buttons and legacy LeftButton*/RightButton* fields.",
                    nameof(request));

            if (request.Buttons is { Count: > 1 })
            {
                System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>();
                foreach (NotificationButton b in request.Buttons)
                    if (!seen.Add(b.Id))
                        throw new ArgumentException($"Duplicate button id '{b.Id}'.", nameof(request));
            }

            NotificationCompletionSource src = new NotificationCompletionSource(request.Id, this, cancellationToken);
            _completions[request.Id] = src;

            IReadOnlyList<NotificationButton> resolvedButtons = NotificationButtonResolver.ResolveButtons(request);
            if (resolvedButtons.Count > 0)
                _buttonsById[request.Id] = resolvedButtons;

            AvaloniaNotificationHost host = ActiveHost;
            if (host == null)
            {
                // No active host — emulate immediate Timeout (matches WPF "no overlay window" behavior)
                src.Complete(NotificationCloseReason.Timeout);
                _completions.TryRemove(request.Id, out _);
                _buttonsById.TryRemove(request.Id, out _);
                return src.Task;
            }

            // Register a completion callback that the host invokes from any close path
            host.RegisterCompletion(request.Id, (reason, buttonId, buttonIndex) =>
            {
                // Map synthetic "left"/"right" → real Buttons[i].Id when available
                string realId = buttonId;
                if (buttonIndex is int idx
                    && _buttonsById.TryGetValue(request.Id, out IReadOnlyList<NotificationButton> realBtns)
                    && idx < realBtns.Count)
                {
                    realId = realBtns[idx].Id;
                }

                _buttonsById.TryRemove(request.Id, out _);
                _completions.TryRemove(request.Id, out _);
                src.Complete(reason, realId, buttonIndex);
            });

            Show(request);
            return src.Task;
        }

        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Buttons == null || request.Buttons.Count == 0)
                throw new ArgumentException(
                    "ShowDialogAsync requires NotificationRequest.Buttons to have at least one button.",
                    nameof(request));

            request.ExpirationTime ??= TimeSpan.MaxValue;
            request.CloseOnClick = false;

            if (request.Buttons.Count > 2)
            {
                switch (fallback)
                {
                    case NotificationDialogFallback.Throw:
                        throw new NotSupportedException(
                            "Avalonia currently renders at most 2 buttons; full N-button rendering is a follow-up. " +
                            "Pass NotificationDialogFallback.ShowFirstAffirmative or SkipSilently.");

                    case NotificationDialogFallback.SkipSilently:
                        return Task.FromResult(new NotificationCloseResult
                        {
                            NotificationId = request.Id,
                            Reason = NotificationCloseReason.Timeout
                        });

                    case NotificationDialogFallback.ShowFirstAffirmative:
                    default:
                        NotificationButton picked = null;
                        foreach (NotificationButton b in request.Buttons)
                            if (b.Role == NotificationButtonRole.Affirmative) { picked = b; break; }
                        if (picked is null) picked = request.Buttons[0];
                        request.Buttons = new[] { picked };
                        break;
                }
            }

            return ShowAsync(request, cancellationToken);
        }
    }
}
