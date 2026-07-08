using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Notification.Core;
using Notification.Core.Internal;

#if ANDROID || IOS || MACCATALYST || WINDOWS
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
#endif

namespace Notification.Maui
{
    /// <summary>
    /// Renders notifications on .NET MAUI platforms using CommunityToolkit Snackbar/Toast for
    /// fire-and-forget notifications and a native ActionSheet for async N-button dialogs.
    /// </summary>
    public class MauiNotificationManager : INotificationService
    {
        private readonly INotificationConfiguration _config;
        private readonly INotificationEventService _events;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeNotifications =
            new ConcurrentDictionary<Guid, CancellationTokenSource>();

        private readonly ConcurrentDictionary<Guid, NotificationCompletionSource> _completions =
            new ConcurrentDictionary<Guid, NotificationCompletionSource>();

        // Programmatic-dismiss channel for the async path: Dismiss(id) / DismissAll cancel these
        // to hide an in-flight Snackbar/Toast and complete the awaited Task as Cancelled.
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _asyncDismissSources =
            new ConcurrentDictionary<Guid, CancellationTokenSource>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MauiNotificationManager"/> class.
        /// </summary>
        public MauiNotificationManager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MauiNotificationManager"/> class
        /// with the specified configuration and event service.
        /// </summary>
        /// <param name="config">The notification configuration.</param>
        /// <param name="events">The event service used to raise lifecycle events.</param>
        public MauiNotificationManager(INotificationConfiguration config, INotificationEventService events)
        {
            _config = config;
            _events = events;
        }

        /// <summary>
        /// Displays the specified notification as a Snackbar or Toast.
        /// </summary>
        /// <param name="request">The notification request to display.</param>
        /// <returns>The unique identifier of the displayed notification.</returns>
        public Guid Show(NotificationRequest request)
        {
            Guid id = request.Id;
            CancellationTokenSource cts = new CancellationTokenSource();
            _activeNotifications.TryAdd(id, cts);

            TimeSpan expiration = request.ExpirationTime ?? _config?.DefaultExpirationTime ?? TimeSpan.FromSeconds(5);

            // Snackbar/Toast duration uses a timer — clamp to safe max to avoid overflow.
            TimeSpan maxSafe = TimeSpan.FromMilliseconds(int.MaxValue);
            if (expiration > maxSafe)
                expiration = maxSafe;

#if ANDROID || IOS || MACCATALYST || WINDOWS
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowSnackbar(request, id, cts.Token, expiration);
            });
#else
            ShowFallback(request, id);
#endif

            _events?.Raise(new NotificationLifecycleEventArgs(
                id, NotificationLifecycleStage.Shown, request.Title, request.Message));

            return id;
        }

#if ANDROID || IOS || MACCATALYST || WINDOWS
        private async void ShowSnackbar(NotificationRequest request, Guid id, CancellationToken cancellationToken, TimeSpan expiration)
        {
            try
            {
                Page anchor = GetCurrentPage();
                if (anchor == null)
                    return;

                string typePrefix = GetTypePrefix(request.Type);
                string displayText = string.IsNullOrEmpty(request.Title)
                    ? $"{typePrefix}{request.Message ?? ""}"
                    : $"{typePrefix}{request.Title}: {request.Message}";

                string actionText = request.LeftButtonContent ?? (request.OnClick != null ? "OK" : null);

                Action clickAction = actionText != null
                    ? () =>
                    {
                        request.OnClick?.Invoke();
                        request.LeftButtonAction?.Invoke();
                        _events?.Raise(new NotificationLifecycleEventArgs(
                            id, NotificationLifecycleStage.Clicked, request.Title, request.Message));
                    }
                    : null;

                SnackbarOptions options = CreateSnackbarOptions(request);

                if (actionText != null)
                {
                    ISnackbar snackbar = Snackbar.Make(displayText, clickAction, actionText, expiration, options);
                    await snackbar.Show(cancellationToken);
                }
                else
                {
                    IToast toast = Toast.Make(displayText, ToastDuration.Short);
                    await toast.Show(cancellationToken);
                }

                CancellationTokenSource removed;
                _activeNotifications.TryRemove(id, out removed);
                removed?.Dispose();

                request.OnClose?.Invoke();
                _events?.Raise(new NotificationLifecycleEventArgs(
                    id, NotificationLifecycleStage.Closed, request.Title, request.Message));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private SnackbarOptions CreateSnackbarOptions(NotificationRequest request)
        {
            Color backgroundColor = GetBackgroundColor(request);
            Color foregroundColor = GetForegroundColor(request);

            return new SnackbarOptions
            {
                BackgroundColor = backgroundColor,
                TextColor = foregroundColor,
                ActionButtonTextColor = foregroundColor,
                Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, Microsoft.Maui.FontWeight.Bold),
                CornerRadius = 8
            };
        }

        private static string GetTypePrefix(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "✅ ",
                NotificationType.Warning => "⚠️ ",
                NotificationType.Error => "❌ ",
                NotificationType.Information => "ℹ️ ",
                _ => ""
            };
        }

        private Color GetBackgroundColor(NotificationRequest request)
        {
            if (request.BackgroundColor.HasValue)
            {
                NotificationColor c = request.BackgroundColor.Value;
                return Color.FromRgba(c.R, c.G, c.B, c.A);
            }

            return request.Type switch
            {
                NotificationType.Success => Color.FromArgb("#2E7D32"),
                NotificationType.Warning => Color.FromArgb("#F57F17"),
                NotificationType.Error => Color.FromArgb("#C62828"),
                NotificationType.Information => Color.FromArgb("#1565C0"),
                _ => Color.FromArgb("#424242")
            };
        }

        private Color GetForegroundColor(NotificationRequest request)
        {
            if (request.ForegroundColor.HasValue)
            {
                NotificationColor c = request.ForegroundColor.Value;
                return Color.FromRgba(c.R, c.G, c.B, c.A);
            }

            return Colors.White;
        }

        private static Page GetCurrentPage()
        {
            Application app = Application.Current;
            if (app == null)
                return null;

            Window window = app.Windows.Count > 0 ? app.Windows[0] : null;
            Page rootPage = window?.Page;

            if (rootPage is Shell shell)
                return shell.CurrentPage;

            if (rootPage is NavigationPage navPage)
                return navPage.CurrentPage;

            return rootPage;
        }
#else
        private void ShowFallback(NotificationRequest request, Guid id)
        {
            string prefix = request.Type switch
            {
                NotificationType.Success => "[OK]",
                NotificationType.Warning => "[WARN]",
                NotificationType.Error => "[ERR]",
                NotificationType.Information => "[INFO]",
                _ => "[NOTE]"
            };
            System.Diagnostics.Debug.WriteLine($"{prefix} {request.Title}: {request.Message}");
        }
#endif

        /// <summary>
        /// Dismisses the notification with the specified identifier.
        /// </summary>
        /// <param name="notificationId">The identifier of the notification to dismiss.</param>
        public void Dismiss(Guid notificationId)
        {
            bool dismissed = false;

            // Async path: cancelling the dismiss source hides the alert and completes the Task as Cancelled.
            if (_asyncDismissSources.TryRemove(notificationId, out CancellationTokenSource asyncCts))
            {
                asyncCts.Cancel();
                asyncCts.Dispose();
                dismissed = true;
            }

            // Legacy fire-and-forget path.
            if (_activeNotifications.TryRemove(notificationId, out CancellationTokenSource cts))
            {
                cts?.Cancel();
                cts?.Dispose();
                dismissed = true;
            }

            if (dismissed)
                _events?.Raise(new NotificationLifecycleEventArgs(
                    notificationId, NotificationLifecycleStage.Dismissed, null, null));
        }

        /// <summary>
        /// Dismisses all currently active notifications.
        /// </summary>
        public void DismissAll()
        {
            foreach (KeyValuePair<Guid, CancellationTokenSource> kvp in _activeNotifications.ToArray())
            {
                Dismiss(kvp.Key);
            }

            foreach (KeyValuePair<Guid, CancellationTokenSource> kvp in _asyncDismissSources.ToArray())
            {
                Dismiss(kvp.Key);
            }
        }

        // Completes the awaited Task exactly once for the given id, and tears down the async dismiss channel.
        private void CompleteOnce(Guid id, NotificationCompletionSource src,
            NotificationCloseReason reason, string buttonId = null, int? buttonIndex = null)
        {
            if (_asyncDismissSources.TryRemove(id, out CancellationTokenSource lcts))
                lcts.Dispose();

            if (_completions.TryRemove(id, out NotificationCompletionSource existing) && existing == src)
                src.Complete(reason, buttonId, buttonIndex);
        }

        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

#pragma warning disable CS0618
            bool hasLegacy = !string.IsNullOrEmpty(request.LeftButtonContent) || !string.IsNullOrEmpty(request.RightButtonContent);
#pragma warning restore CS0618
            if (request.Buttons is { Count: > 0 } && hasLegacy)
                throw new ArgumentException("NotificationRequest specifies both Buttons and legacy LeftButton*/RightButton* fields.", nameof(request));

            if (request.Buttons is { Count: > 1 })
            {
                HashSet<string> seen = new HashSet<string>();
                foreach (NotificationButton b in request.Buttons)
                    if (!seen.Add(b.Id))
                        throw new ArgumentException($"Duplicate button id '{b.Id}'.", nameof(request));
            }

            NotificationCompletionSource src = new NotificationCompletionSource(request.Id, this, cancellationToken);
            _completions[request.Id] = src;

            IReadOnlyList<NotificationButton> resolved = NotificationButtonResolver.ResolveButtons(request);

            TimeSpan expiration = request.ExpirationTime ?? _config?.DefaultExpirationTime ?? TimeSpan.FromSeconds(5);
            TimeSpan maxSafe = TimeSpan.FromMilliseconds(int.MaxValue);
            if (expiration > maxSafe) expiration = maxSafe;

#if ANDROID || IOS || MACCATALYST || WINDOWS
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await ShowInternalAsync(request, resolved, cancellationToken, expiration, src);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Notification.Maui] ShowAsync error: {ex}");
                    CompleteOnce(request.Id, src, NotificationCloseReason.Timeout);
                }
            });
#else
            ShowFallback(request, request.Id);
            CompleteOnce(request.Id, src, NotificationCloseReason.Timeout);
#endif

            _events?.Raise(new NotificationLifecycleEventArgs(
                request.Id, NotificationLifecycleStage.Shown, request.Title, request.Message));

            return src.Task;
        }

#if ANDROID || IOS || MACCATALYST || WINDOWS
        private async Task ShowInternalAsync(
            NotificationRequest request,
            IReadOnlyList<NotificationButton> buttons,
            CancellationToken cancellationToken,
            TimeSpan expiration,
            NotificationCompletionSource src)
        {
            Page page = GetCurrentPage();
            if (page == null)
            {
                CompleteOnce(request.Id, src, NotificationCloseReason.Timeout);
                return;
            }

            // Programmatic-dismiss channel (Dismiss(id) / DismissAll cancel this).
            CancellationTokenSource dismissCts = new CancellationTokenSource();
            _asyncDismissSources[request.Id] = dismissCts;

            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, dismissCts.Token);
            CancellationToken effective = linked.Token;

            if (buttons.Count > 0)
                await ShowActionSheetAsync(page, request, buttons, effective, src);
            else
                await ShowToastAsync(request, expiration, effective, src);
        }

        // Interactive dialogs use a native action sheet. Unlike the CommunityToolkit Snackbar action
        // button — whose callback does not fire on Windows — DisplayActionSheet returns the chosen text
        // reliably on every platform and supports any number of buttons.
        private async Task ShowActionSheetAsync(
            Page page,
            NotificationRequest request,
            IReadOnlyList<NotificationButton> buttons,
            CancellationToken effective,
            NotificationCompletionSource src)
        {
            string typePrefix = GetTypePrefix(request.Type);
            string header = string.IsNullOrEmpty(request.Title)
                ? $"{typePrefix}{request.Message ?? ""}"
                : $"{typePrefix}{request.Title}";
            string sheetTitle = (!string.IsNullOrEmpty(request.Title) && !string.IsNullOrEmpty(request.Message))
                ? $"{header}\n{request.Message}"
                : header;

            string[] texts = new string[buttons.Count];
            for (int i = 0; i < buttons.Count; i++)
                texts[i] = buttons[i].Content;

            Task<string> sheetTask = page.DisplayActionSheetAsync(sheetTitle, null, null, texts);

            // DisplayActionSheet has no CancellationToken overload; race a registration instead.
            TaskCompletionSource<bool> cancelTcs =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration reg = effective.Register(() => cancelTcs.TrySetResult(true));

            Task finished = await Task.WhenAny(sheetTask, cancelTcs.Task);

            NotificationCloseReason reason;
            string buttonId = null;
            int? buttonIndex = null;

            if (finished == sheetTask && sheetTask.Status == TaskStatus.RanToCompletion)
            {
                string chosen = sheetTask.Result;
                int idx = -1;
                if (chosen != null)
                    for (int i = 0; i < buttons.Count; i++)
                        if (string.Equals(buttons[i].Content, chosen, StringComparison.Ordinal)) { idx = i; break; }

                if (idx >= 0)
                {
                    reason = NotificationCloseReason.ActionButton;
                    buttonId = buttons[idx].Id;
                    buttonIndex = idx;
                    request.OnClick?.Invoke();
                    _events?.Raise(new NotificationLifecycleEventArgs(
                        request.Id, NotificationLifecycleStage.Clicked, request.Title, request.Message));
                }
                else
                {
                    // Dismissed without choosing one of the listed buttons.
                    reason = NotificationCloseReason.Timeout;
                }
            }
            else
            {
                reason = NotificationCloseReason.Cancelled;
            }

            request.OnClose?.Invoke();
            _events?.Raise(new NotificationLifecycleEventArgs(
                request.Id, NotificationLifecycleStage.Closed, request.Title, request.Message, reason));

            CompleteOnce(request.Id, src, reason, buttonId, buttonIndex);
        }

        // Non-interactive notifications use a transient Toast; the awaited Task resolves on the
        // expiration timer or on cancellation (CommunityToolkit exposes no user-dismiss callback).
        private async Task ShowToastAsync(
            NotificationRequest request,
            TimeSpan expiration,
            CancellationToken effective,
            NotificationCompletionSource src)
        {
            string typePrefix = GetTypePrefix(request.Type);
            string displayText = string.IsNullOrEmpty(request.Title)
                ? $"{typePrefix}{request.Message ?? ""}"
                : $"{typePrefix}{request.Title}: {request.Message}";

            IToast toast = Toast.Make(displayText, ToastDuration.Short);
            _ = SafeShowAsync(toast, effective);

            await SafeDelayAsync(expiration, effective);

            NotificationCloseReason reason = effective.IsCancellationRequested
                ? NotificationCloseReason.Cancelled
                : NotificationCloseReason.Timeout;

            try { await toast.Dismiss(CancellationToken.None); } catch { /* already gone */ }

            request.OnClose?.Invoke();
            _events?.Raise(new NotificationLifecycleEventArgs(
                request.Id, NotificationLifecycleStage.Closed, request.Title, request.Message, reason));

            CompleteOnce(request.Id, src, reason);
        }

        private static async Task SafeShowAsync(IAlert alert, CancellationToken token)
        {
            try { await alert.Show(token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Notification.Maui] alert.Show error: {ex}"); }
        }

        private static async Task SafeDelayAsync(TimeSpan delay, CancellationToken token)
        {
            try { await Task.Delay(delay, token); }
            catch (OperationCanceledException) { }
        }
#endif

        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Buttons == null || request.Buttons.Count == 0)
                throw new ArgumentException("ShowDialogAsync requires Buttons.Count >= 1.", nameof(request));

            request.ExpirationTime ??= TimeSpan.MaxValue;
            request.CloseOnClick = false;

            // MAUI renders dialogs with a native action sheet, which supports any number of buttons.
            // The fallback exists for button-limited platforms and therefore does not apply here —
            // all declared buttons are shown regardless of the requested fallback.
            return ShowAsync(request, cancellationToken);
        }
    }
}
