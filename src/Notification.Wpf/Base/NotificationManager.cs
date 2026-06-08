using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using Notification.Core;
using Notification.Wpf.Adapters;
using Notification.Wpf.Base;
using Notification.Wpf.Base.Interfaces.Options;
using Notification.Wpf.Classes;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;

using Notifications.Wpf.Annotations;
using Notifications.Wpf.ViewModels;

using WpfNotification = Notification.Wpf.Controls.Notification;

namespace Notification.Wpf
{
    /// <inheritdoc />
    public class NotificationManager : INotificationManager
    {
        private readonly Dispatcher _dispatcher;
        private readonly INotificationConfiguration _config;
        private readonly INotificationEventService _events;
        private readonly ConcurrentDictionary<Guid, Action> _dismissActions = new();
        private static readonly List<NotificationArea> Areas = new();
        private static NotificationsOverlayWindow _window;

        /// <summary>
        /// Initialize new notification manager
        /// </summary>
        /// <param name="dispatcher">dispatcher for manager (can be null)</param>
        public NotificationManager(Dispatcher dispatcher = null)
        {
            dispatcher ??= Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Initialize new notification manager with DI configuration
        /// </summary>
        public NotificationManager(INotificationConfiguration config, INotificationEventService events, Dispatcher dispatcher = null)
        {
            _config = config;
            _events = events;

            dispatcher ??= Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _dispatcher = dispatcher;
        }

        #region Messages


        /// <inheritdoc />
        public void Show(object content, string areaName = "", TimeSpan? expirationTime = null, Action onClick = null,
            Action onClose = null, bool CloseOnClick = true, bool ShowXbtn = true)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(
                    new Action(() => Show(content, areaName, expirationTime, onClick, onClose, CloseOnClick, ShowXbtn)));
                return;
            }

            areaName ??= "";

            ShowContent(content, expirationTime, areaName, onClick, onClose, CloseOnClick, ShowXbtn);
        }

        /// <inheritdoc />
        public void Show(string title, string message, NotificationType type = NotificationType.None, string areaName = "", TimeSpan? expirationTime = null,
            Action onClick = null,
            Action onClose = null,
            Action LeftButton = null,
            string LeftButtonText = null,
            Action RightButton = null,
            string RightButtonText = null,
            NotificationTextTrimType trim = NotificationTextTrimType.NoTrim, uint RowsCountWhenTrim = 2, bool CloseOnClick = true,
            TextContentSettings TitleSettings = null, TextContentSettings MessageSettings = null, bool ShowXbtn = true, object icon = null)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(
                    new Action(
                        () => Show(
                            title, message, type,
                            areaName,
                            expirationTime,
                            onClick, onClose,
                            LeftButton, LeftButtonText,
                            RightButton, RightButtonText,
                            trim, RowsCountWhenTrim,
                            CloseOnClick,
                            TitleSettings, MessageSettings,
                            ShowXbtn, icon)));
                return;
            }

            var content = new NotificationContent
            {
                Type = type,
                TrimType = trim,
                RowsCount = RowsCountWhenTrim,
                LeftButtonAction = LeftButton,
                LeftButtonContent = LeftButtonText,
                RightButtonAction = RightButton,
                RightButtonContent = RightButtonText,
                Message = message,
                Title = title,
                CloseOnClick = CloseOnClick,
                MessageTextSettings = MessageSettings ?? NotificationConstants.MessageSettings,
                TitleTextSettings = TitleSettings ?? NotificationConstants.TitleSettings,
                Icon = icon
            };

            ShowContent(content, expirationTime, areaName, onClick, onClose, CloseOnClick, ShowXbtn);
        }
        /// <inheritdoc />
        public void Show(
            string message, NotificationType type = NotificationType.None,
            string areaName = "", TimeSpan? expirationTime = null,
            NotificationTextTrimType trim = NotificationTextTrimType.NoTrim, uint RowsCountWhenTrim = 1,
            bool CloseOnClick = true,
            TextContentSettings MessageSettings = null,
            bool ShowXbtn = true,
            object icon = null)
        {

            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(
                    new Action(
                        () => Show(
                            message, type,
                            areaName,
                            expirationTime,
                            trim, RowsCountWhenTrim,
                            CloseOnClick,
                            MessageSettings,
                            ShowXbtn,
                            icon)));
                return;
            }

            var content = new NotificationContent
            {
                Type = type,
                TrimType = trim,
                RowsCount = RowsCountWhenTrim,
                Message = message,
                CloseOnClick = CloseOnClick,
                MessageTextSettings = MessageSettings ?? NotificationConstants.MessageSettings,
                Icon = icon
            };

            ShowContent(content, expirationTime, areaName, null, null, CloseOnClick, ShowXbtn);
        }
        /// <inheritdoc />
        public void Show(
            [NotNull] Exception e,
            string areaName = "",
            TimeSpan? expirationTime = null, uint RowsCountWhenTrim = 5,
            TextContentSettings MessageSettings = null,
            bool ShowXbtn = true) =>
            Show(
                $"{e.Message}\n\r{e}",
                NotificationType.Error,
                areaName,
                expirationTime ?? TimeSpan.MaxValue,
                NotificationTextTrimType.AttachIfMoreRows,
                RowsCountWhenTrim, true, MessageSettings ?? NotificationConstants.MessageSettings, ShowXbtn);

        /// <inheritdoc />
        public void ShowCancellation(NotificationType type = NotificationType.Warning, string areaName = "",
            TextContentSettings MessageSettings = null,
            bool ShowXbtn = true)
            => Show(NotificationConstants.CancellationMessage, type, areaName, MessageSettings: MessageSettings ?? NotificationConstants.MessageSettings, ShowXbtn: ShowXbtn);

        #endregion

        #region Progress

        /// <inheritdoc />
        public NotifierProgress<NotificationProgressReport> ShowProgressBar(ProgressBarOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

#pragma warning disable CS0618 // delegating to the obsolete positional overload
            return ShowProgressBar(
                options.Title,
                options.ShowCancelButton,
                options.ShowProgress,
                options.AreaName ?? "",
                options.TrimText,
                options.DefaultRowsCount,
                options.BaseWaitingMessage,
                options.IsCollapse,
                options.TitleWhenCollapsed,
                options.BackgroundColor?.ToBrush(),
                options.ForegroundColor?.ToBrush(),
                options.ProgressColor?.ToBrush(),
                options.Icon,
                options.TitleSettings?.ToWpfSettings(),
                options.MessageSettings?.ToWpfSettings(),
                options.ShowCloseButton);
#pragma warning restore CS0618
        }

        /// <inheritdoc />
        [Obsolete("Use ShowProgressBar(ProgressBarOptions) instead — it replaces the long positional parameter list with a configurable options object.")]
        public NotifierProgress<NotificationProgressReport> ShowProgressBar(string Title = null,
            bool ShowCancelButton = true,
            bool ShowProgress = true,
            string areaName = "",
            bool TrimText = false, uint DefaultRowsCount = 1U,
            string BaseWaitingMessage = "Calculation time",
            bool IsCollapse = false, bool TitleWhenCollapsed = true,
            Brush background = null, Brush foreground = null, Brush progressColor = null,
            object icon = default,
            TextContentSettings TitleSettings = null, TextContentSettings MessageSettings = null, bool ShowXbtn = true)
        {

            if (!_dispatcher.CheckAccess())
            {
#pragma warning disable CS0618 // marshalling the same obsolete call onto the dispatcher
                return _dispatcher.Invoke(
                    () => ShowProgressBar(
                        Title,
                        ShowCancelButton, ShowProgress,
                        areaName,
                        TrimText, DefaultRowsCount,
                        BaseWaitingMessage,
                        IsCollapse, TitleWhenCollapsed,
                        background, foreground, progressColor,
                        icon,
                        TitleSettings, MessageSettings,
                        ShowXbtn));
#pragma warning restore CS0618
            }

            var model = new NotificationProgressViewModel(
                ShowCancelButton, ShowProgress,
                TrimText, DefaultRowsCount,
                BaseWaitingMessage,
                IsCollapse, TitleWhenCollapsed,
                background, foreground, progressColor, icon,
                TitleSettings ?? NotificationConstants.TitleSettings, MessageSettings ?? NotificationConstants.MessageSettings);

            if (Title != null)
                model.Title = Title;

            ShowContent(model, areaName: areaName, ShowXbtn: ShowXbtn);
            return model.NotifierProgress;
        }
        /// <inheritdoc />
        public NotifierProgress<NotificationProgressReport> ShowProgressBar(ICustomizedNotification content,
            bool ShowCancelButton = true,
            bool ShowProgress = true,
            string areaName = "",
            string BaseWaitingMessage = "Calculation time",
            bool IsCollapse = false,
            bool TitleWhenCollapsed = true,
            Brush progressColor = null,
            bool ShowXbtn = true)
        {

            if (!_dispatcher.CheckAccess())
            {
                return _dispatcher.Invoke(
                    () => ShowProgressBar(
                        content,
                        ShowCancelButton, ShowProgress,
                        areaName,
                        BaseWaitingMessage,
                        IsCollapse,
                        TitleWhenCollapsed,
                        progressColor,
                        ShowXbtn));
            }

            var model = new NotificationProgressViewModel(content,
                ShowCancelButton,
                ShowProgress,
                BaseWaitingMessage,
                IsCollapse,
                TitleWhenCollapsed,
                progressColor);

            ShowContent(model, areaName: areaName, ShowXbtn: ShowXbtn);
            return model.NotifierProgress;
        }

        #endregion

        #region Buttons

        /// <inheritdoc />
        public void ShowFilePopUpMessage(
            string FilePath, bool ShowFile = true, bool ShowDirectory = true,
            TimeSpan? ExpirationTime = null, string AreaName = "",
            ICustomizedOptions options = null,
            NotificationImage image = null,
            bool ShowXbtn = true) =>
            ShowButtonWindow($"{NotificationConstants.OpenFileMessage}?", null,
                ShowFile ? () =>
                {
                    try
                    {
                        new Process { StartInfo = new ProcessStartInfo(FilePath) { UseShellExecute = true } }.Start();
                    }
                    catch (Exception exc)
                    {
                        Show(exc);
                    }
                }
        : null
              , ShowFile ? NotificationConstants.OpenFileMessage : null,
                ShowDirectory ? () =>
                {
                    try
                    {
                        new Process
                        {
                            StartInfo = new ProcessStartInfo(
                                Path.GetDirectoryName(FilePath)
                                ?? throw new ArgumentNullException(
                                    nameof(FilePath),
                                    "File path can`t be null"))
                            {
                                UseShellExecute = true
                            }
                        }.Start();
                    }
                    catch (Exception exc)
                    {
                        Show(exc);
                    }

                }
        : null, ShowDirectory ? NotificationConstants.OpenFolderMessage : null, ExpirationTime, AreaName, options, image, ShowXbtn);

        /// <inheritdoc />
        public void ShowButtonWindow(string Message, [CanBeNull] string Title = null,
        [CanBeNull] Action LeftButtonAction = null, string LeftButtonContent = null,
            [CanBeNull] Action RightButtonAction = null, string RightButtonContent = null,
            TimeSpan? ExpirationTime = null, string AreaName = "",
            ICustomizedOptions options = null,
            NotificationImage Image = null,
            bool ShowXbtn = true)
        {
            Show(NotificationContent.GetValidContent(
                Title,
                Message,
                NotificationType.None,
                LeftButtonAction,
                LeftButtonContent,
                RightButtonAction,
                RightButtonContent,
                Image,
                false,
                options),
                AreaName, ExpirationTime, null, null, false, ShowXbtn);
        }


        #endregion


        /// <summary>
        /// Запуск отображения в зависимости от типа контента
        /// </summary>
        /// <param name="content">контент</param>
        /// <param name="expirationTime">время отображения</param>
        /// <param name="areaName">зона отображения</param>
        /// <param name="onClick">действие при клике</param>
        /// <param name="onClose">действие при закрытии</param>
        /// <param name="CloseOnClick">Закрыть сообщение при клике по телу</param>
        /// <param name="ShowXbtn">Show X (close) btn</param>
        /// <param name="onCreated">Optional callback invoked with each created notification, used to register a programmatic dismiss handle.</param>
        static void ShowContent(object content, TimeSpan? expirationTime = null, string areaName = "",
            Action onClick = null, Action onClose = null, bool CloseOnClick = true, bool ShowXbtn = true,
            Action<WpfNotification> onCreated = null)
        {
            expirationTime ??= TimeSpan.FromSeconds(5);

            if (areaName == string.Empty && _window == null)
            {
                var workArea = SystemParameters.WorkArea;

                _window = new NotificationsOverlayWindow
                {
                    Left = workArea.Left,
                    Top = workArea.Top,
                    Width = workArea.Width,
                    Height = workArea.Height,
                    Topmost = NotificationConstants.OverlayWindowTopmost,
                    CollapseProgressAutoIfMoreMessages = NotificationConstants.CollapseProgressIfMoreRows,
                    MaxWindowItems = NotificationConstants.NotificationsOverlayWindowMaxCount,
                    MessagePosition = NotificationConstants.MessagePosition
                };
                // Drop the reference as soon as the window starts closing so a concurrent
                // Show() call recreates a fresh window instead of touching a closing one (issue #66).
                _window.Closing += (_, _) => { _window = null; };
                _window.Closed += (_, _) => { _window = null; };
            }

            if (Areas != null && _window is { IsVisible: false })
            {
                try
                {
                    _window.Show();
                }
                catch (InvalidOperationException)
                {
                    // The overlay window is mid-close on another path — discard it; the next call recreates one.
                    _window = null;
                    return;
                }
            }

            if (Areas == null) return;
            foreach (var area in Areas.Where(a => a.Name == areaName))
            {
                switch (content)
                {
                    case NotificationProgressViewModel progress:
                        area.Show(progress, ShowXbtn);
                        break;
                    default:
                        area.Show(content, (TimeSpan)expirationTime, onClick, onClose, CloseOnClick, ShowXbtn, onCreated);
                        break;
                }
            }
        }

        #region INotificationService

        /// <inheritdoc />
        public Guid Show(NotificationRequest request) => ShowCore(request, extraOnCreated: null);

        /// <summary>
        /// Core implementation shared by <see cref="Show(NotificationRequest)"/> and
        /// <see cref="ShowAsync"/>. The optional <paramref name="extraOnCreated"/> callback
        /// is invoked with each <see cref="WpfNotification"/> instance right after it is
        /// created, so callers can subscribe to its events before the timer starts.
        /// </summary>
        private Guid ShowCore(NotificationRequest request, Action<WpfNotification> extraOnCreated)
        {
            if (!_dispatcher.CheckAccess())
            {
                return _dispatcher.Invoke(() => ShowCore(request, extraOnCreated));
            }

            Action onClick = request.OnClick;
            Action onClose = request.OnClose;
            Guid notificationId = request.Id;

            object content;
            if (request.Content != null)
            {
                // Arbitrary platform-specific content (for example, a WPF control) is shown as-is.
                content = request.Content;
            }
            else
            {
                Brush background = request.BackgroundColor.HasValue
                    ? request.BackgroundColor.Value.ToBrush()
                    : GetBackgroundForType(request.Type);

                Brush foreground = request.ForegroundColor.HasValue
                    ? request.ForegroundColor.Value.ToBrush()
                    : null;

                if (request.Extensions != null)
                {
                    if (request.Extensions.TryGetValue("Wpf.Background", out object wpfBg) && wpfBg is Brush bgBrush)
                        background = bgBrush;
                    if (request.Extensions.TryGetValue("Wpf.Foreground", out object wpfFg) && wpfFg is Brush fgBrush)
                        foreground = fgBrush;
                }

                TextContentSettings titleSettings = request.TitleSettings != null
                    ? request.TitleSettings.ToWpfSettings()
                    : NotificationConstants.TitleSettings;

                TextContentSettings messageSettings = request.MessageSettings != null
                    ? request.MessageSettings.ToWpfSettings()
                    : NotificationConstants.MessageSettings;

                if (request.Extensions != null)
                {
                    if (request.Extensions.TryGetValue("Wpf.TitleSettings", out object wpfTs) && wpfTs is TextContentSettings ts)
                        titleSettings = ts;
                    if (request.Extensions.TryGetValue("Wpf.MessageSettings", out object wpfMs) && wpfMs is TextContentSettings ms)
                        messageSettings = ms;
                }

                NotificationImage image = null;
                if (request.Extensions != null && request.Extensions.TryGetValue("Wpf.Image", out object wpfImg) && wpfImg is NotificationImage ni)
                    image = ni;

#pragma warning disable CS0618
                content = new NotificationContent
                {
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type,
                    TrimType = request.TrimType,
                    RowsCount = request.RowsCount,
                    CloseOnClick = request.CloseOnClick,
                    LeftButtonAction = request.LeftButtonAction,
                    LeftButtonContent = request.LeftButtonContent,
                    RightButtonAction = request.RightButtonAction,
                    RightButtonContent = request.RightButtonContent,
                    RightClickAction = request.OnRightClick,
                    Background = background,
                    Foreground = foreground,
                    TitleTextSettings = titleSettings,
                    MessageTextSettings = messageSettings,
                    Icon = request.Icon,
                    IconForeground = request.IconColor?.ToBrush(),
                    Image = image
                };
#pragma warning restore CS0618
            }

            // Always clean up the dismiss registration when the notification closes,
            // and raise the Closed lifecycle event when an event service is available.
            Action originalOnClose = onClose;
            onClose = () =>
            {
                originalOnClose?.Invoke();
                _dismissActions.TryRemove(notificationId, out _);
                _events?.Raise(new NotificationLifecycleEventArgs(
                    notificationId, NotificationLifecycleStage.Closed, request.Title, request.Message));
            };

            List<WpfNotification> created = new List<WpfNotification>();
            Action<WpfNotification> combinedOnCreated = n =>
            {
                created.Add(n);
                extraOnCreated?.Invoke(n);
            };
            ShowContent(content, request.ExpirationTime, request.AreaName ?? "", onClick, onClose,
                request.CloseOnClick, request.ShowCloseButton, combinedOnCreated);

            // Register a dismiss action so Dismiss(id) / DismissAll() can close this notification (issue #48).
            if (created.Count > 0)
                _dismissActions[notificationId] = () =>
                {
                    foreach (WpfNotification n in created)
                        if (!n.IsClosing)
                        {
                            n.PendingCloseReason ??= global::Notification.Core.NotificationCloseReason.Cancelled;
                            n.Close();
                        }
                };

            _events?.Raise(new NotificationLifecycleEventArgs(
                notificationId, NotificationLifecycleStage.Shown, request.Title, request.Message));

            return notificationId;
        }

        /// <inheritdoc />
        public void Dismiss(Guid notificationId)
        {
            if (_dismissActions.TryRemove(notificationId, out Action dismissAction))
            {
                if (!_dispatcher.CheckAccess())
                {
                    _dispatcher.BeginInvoke(dismissAction);
                    return;
                }
                dismissAction();
            }
        }

        /// <inheritdoc />
        public void DismissAll()
        {
            foreach (KeyValuePair<Guid, Action> kvp in _dismissActions.ToArray())
            {
                Dismiss(kvp.Key);
            }
        }

        private static Brush GetBackgroundForType(NotificationType type) => type switch
        {
            NotificationType.Success => NotificationConstants.SuccessBackgroundColor,
            NotificationType.Warning => NotificationConstants.WarningBackgroundColor,
            NotificationType.Error => NotificationConstants.ErrorBackgroundColor,
            NotificationType.Information => NotificationConstants.InformationBackgroundColor,
            _ => NotificationConstants.DefaultBackgroundColor
        };

        #endregion

        private readonly ConcurrentDictionary<Guid, global::Notification.Core.Internal.NotificationCompletionSource> _completions = new();

        private readonly ConcurrentDictionary<Guid, IReadOnlyList<global::Notification.Core.NotificationButton>> _buttonsById = new();

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
                foreach (global::Notification.Core.NotificationButton b in request.Buttons)
                    if (!seen.Add(b.Id))
                        throw new ArgumentException($"Duplicate button id '{b.Id}' in NotificationRequest.Buttons.", nameof(request));
            }

            global::Notification.Core.Internal.NotificationCompletionSource src = new global::Notification.Core.Internal.NotificationCompletionSource(request.Id, this, cancellationToken);
            _completions[request.Id] = src;

            ProjectButtonsOntoLegacySlots(request);

            IReadOnlyList<global::Notification.Core.NotificationButton> resolvedButtons =
                global::Notification.Core.Internal.NotificationButtonResolver.ResolveButtons(request);
            if (resolvedButtons.Count > 0)
                _buttonsById[request.Id] = resolvedButtons;

            ShowCore(request, extraOnCreated: n =>
            {
                RoutedEventHandler handler = null;
                handler = (sender, e) =>
                {
                    n.NotificationClosed -= handler;
                    if (!_completions.TryRemove(request.Id, out _)) return;

                    global::Notification.Core.NotificationCloseReason reason;
                    string buttonId = null;
                    int? buttonIndex = null;
                    if (e is NotificationClosedRoutedEventArgs rea)
                    {
                        reason = rea.Reason;
                        buttonId = rea.ButtonId;
                        buttonIndex = rea.ButtonIndex;
                    }
                    else
                    {
                        reason = global::Notification.Core.NotificationCloseReason.Timeout;
                    }

                    // Map synthetic "left"/"right" index back to real Buttons[i].Id when available.
                    if (buttonIndex is int idx
                        && _buttonsById.TryGetValue(request.Id, out IReadOnlyList<global::Notification.Core.NotificationButton> realBtns)
                        && idx < realBtns.Count)
                    {
                        buttonId = realBtns[idx].Id;
                    }
                    _buttonsById.TryRemove(request.Id, out _);

                    src.Complete(reason, buttonId, buttonIndex);
                };
                n.NotificationClosed += handler;
            });

            return src.Task;
        }

        private static void ProjectButtonsOntoLegacySlots(NotificationRequest request)
        {
            IReadOnlyList<global::Notification.Core.NotificationButton> resolved =
                global::Notification.Core.Internal.NotificationButtonResolver.ResolveButtons(request);

            if (resolved.Count == 0) return;

#pragma warning disable CS0618
            if (string.IsNullOrEmpty(request.LeftButtonContent) && resolved.Count >= 1)
                request.LeftButtonContent = resolved[0].Content;

            if (string.IsNullOrEmpty(request.RightButtonContent) && resolved.Count >= 2)
                request.RightButtonContent = resolved[1].Content;
#pragma warning restore CS0618

            if (resolved.Count > 2)
                Debug.WriteLine(
                    $"[Notification.Wpf] Dropping {resolved.Count - 2} button(s) beyond the legacy 2-slot UI; full N-button rendering is a follow-up.");
        }

        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Buttons == null || request.Buttons.Count == 0)
                throw new ArgumentException("ShowDialogAsync requires NotificationRequest.Buttons to have at least one button.", nameof(request));

            request.ExpirationTime ??= TimeSpan.MaxValue;
            request.CloseOnClick = false;

            if (request.Buttons.Count > 2)
            {
                switch (fallback)
                {
                    case NotificationDialogFallback.Throw:
                        throw new NotSupportedException("WPF currently renders at most 2 buttons; full N-button rendering is a follow-up. Pass NotificationDialogFallback.ShowFirstAffirmative or SkipSilently.");
                    case NotificationDialogFallback.SkipSilently:
                        return Task.FromResult(new NotificationCloseResult
                        {
                            NotificationId = request.Id,
                            Reason = NotificationCloseReason.Timeout
                        });
                    case NotificationDialogFallback.ShowFirstAffirmative:
                    default:
                        global::Notification.Core.NotificationButton picked = null;
                        foreach (global::Notification.Core.NotificationButton b in request.Buttons)
                            if (b.Role == global::Notification.Core.NotificationButtonRole.Affirmative) { picked = b; break; }
                        if (picked is null) picked = request.Buttons[0];
                        request.Buttons = new[] { picked };
                        break;
                }
            }

            return ShowAsync(request, cancellationToken);
        }

        internal static void AddArea(NotificationArea area) => Areas.Add(area);
        internal static void RemoveArea(NotificationArea area) => Areas.Remove(area);
    }
}