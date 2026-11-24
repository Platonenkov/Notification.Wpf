using System;
using System.Windows.Input;
using System.Windows.Media;

using Notification.Core;
using Notification.Wpf;
using Notification.Wpf.Base;
using Notification.Wpf.Classes;
using Notification.Wpf.Controls;
using Notifications.Wpf.Command;
using Notifications.Wpf.ViewModels.Base;

namespace Notifications.Wpf.ViewModels
{
    /// <summary>
    /// The data a notification control binds to, created once per show of a <see cref="NotificationContent"/>.
    /// It becomes <see cref="System.Windows.Controls.ContentControl.Content"/> of the notification, so the caller's
    /// content object is never modified and showing the same content twice yields two independent notifications.
    /// </summary>
    /// <remarks>
    /// Values are copied from <see cref="Source"/> when the view model is created; later changes to the source
    /// are not reflected. Templates for subclasses of <see cref="NotificationContent"/> reach their own
    /// properties through <see cref="Source"/>, for example <c>{Binding Source.Extra}</c>.
    /// </remarks>
    public sealed class NotificationViewModel : ViewModel, ICustomizedNotification
    {
        private readonly NotificationSession _session;
        private string _title;
        private string _message;
        private NotificationType _type;
        private NotificationImage _image;
        private object _icon;
        private Brush _iconForeground;
        private Brush _background;
        private Brush _foreground;
        private TextContentSettings _titleTextSettings;
        private TextContentSettings _messageTextSettings;
        private NotificationTextTrimType _trimType;
        private uint _rowsCount;
        private object _leftButtonContent;
        private object _rightButtonContent;

        internal NotificationViewModel(NotificationContent source, NotificationSession session)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            _title = source.Title;
            _message = source.Message;
            _type = source.Type;
            _image = source.Image;
            _icon = source.Icon;
            _iconForeground = source.IconForeground;
            _background = source.Background;
            _foreground = source.Foreground;
            _titleTextSettings = source.TitleTextSettings;
            _messageTextSettings = source.MessageTextSettings;
            _trimType = source.TrimType;
            _rowsCount = source.RowsCount;
            _leftButtonContent = source.LeftButtonContent;
            _rightButtonContent = source.RightButtonContent;

            // A null command keeps the button collapsed in the default template; the actions are captured
            // here so the command never reads a source that the caller changed afterwards.
            LeftButtonCommand = CreateCommand(source.LeftButtonAction, "left", 0);
            RightButtonCommand = CreateCommand(source.RightButtonAction, "right", 1);
        }

        /// <summary>Gets the content this view model was created from, including any subclass members.</summary>
        public NotificationContent Source { get; }

        /// <summary>Gets the command bound to the left button, or <see langword="null"/> when the source has no left action.</summary>
        public ICommand LeftButtonCommand { get; }

        /// <summary>Gets the command bound to the right button, or <see langword="null"/> when the source has no right action.</summary>
        public ICommand RightButtonCommand { get; }

        /// <inheritdoc />
        public string Title { get => _title; set => Set(ref _title, value); }

        /// <inheritdoc />
        public string Message { get => _message; set => Set(ref _message, value); }

        /// <summary>Gets or sets the notification type that selects the built-in icon and colors.</summary>
        public NotificationType Type { get => _type; set => Set(ref _type, value); }

        /// <summary>Gets or sets the image shown with the notification.</summary>
        public NotificationImage Image { get => _image; set => Set(ref _image, value); }

        /// <inheritdoc />
        public object Icon { get => _icon; set => Set(ref _icon, value); }

        /// <inheritdoc />
        public Brush IconForeground { get => _iconForeground; set => Set(ref _iconForeground, value); }

        /// <inheritdoc />
        public Brush Background { get => _background; set => Set(ref _background, value); }

        /// <inheritdoc />
        public Brush Foreground { get => _foreground; set => Set(ref _foreground, value); }

        /// <inheritdoc />
        public TextContentSettings TitleTextSettings { get => _titleTextSettings; set => Set(ref _titleTextSettings, value); }

        /// <inheritdoc />
        public TextContentSettings MessageTextSettings { get => _messageTextSettings; set => Set(ref _messageTextSettings, value); }

        /// <inheritdoc />
        public NotificationTextTrimType TrimType { get => _trimType; set => Set(ref _trimType, value); }

        /// <inheritdoc />
        public uint RowsCount { get => _rowsCount; set => Set(ref _rowsCount, value); }

        /// <summary>Gets or sets the content of the left button.</summary>
        public object LeftButtonContent { get => _leftButtonContent; set => Set(ref _leftButtonContent, value); }

        /// <summary>Gets or sets the content of the right button.</summary>
        public object RightButtonContent { get => _rightButtonContent; set => Set(ref _rightButtonContent, value); }

        private ICommand CreateCommand(Action action, string buttonId, int buttonIndex)
        {
            if (action == null)
                return null;

            return new LamdaCommand(_ => Execute(action, buttonId, buttonIndex));
        }

        private void Execute(Action action, string buttonId, int buttonIndex)
        {
            // A close that already has a reason (timeout, eviction, an earlier button) wins; this also makes
            // a double click run the action once. A close without a reason is the CloseOnClick attached
            // property of a custom template, whose Click handler runs before this Command, so it must pass.
            if (_session.IsClosing && _session.PendingReason != null)
                return;

            _session.PendingButtonId ??= buttonId;
            _session.PendingButtonIndex ??= buttonIndex;
            _session.RequestClose(NotificationCloseReason.ActionButton);

            // After the close has started: an action that throws still closes the notification and the
            // exception reaches the dispatcher, as it did before.
            action();
        }
    }
}
