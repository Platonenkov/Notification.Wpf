using System;
using System.Collections.Generic;

namespace Notification.Core
{
    /// <summary>
    /// Provides a fluent API for constructing <see cref="NotificationRequest"/> instances.
    /// </summary>
    public class NotificationBuilder
    {
        /// <summary>The notification title.</summary>
        protected string _title;
        /// <summary>The notification message body.</summary>
        protected string _message;
        /// <summary>The notification type.</summary>
        protected NotificationType _type = NotificationType.None;
        /// <summary>The name of the target notification area.</summary>
        protected string _areaName = "";
        /// <summary>The time after which the notification expires; <c>null</c> means never.</summary>
        protected TimeSpan? _expirationTime;
        /// <summary>Indicates whether the notification closes when clicked.</summary>
        protected bool _closeOnClick = true;
        /// <summary>Indicates whether the close button is shown.</summary>
        protected bool _showCloseButton = true;
        /// <summary>The notification priority.</summary>
        protected NotificationPriority _priority = NotificationPriority.Normal;
        /// <summary>The grouping key used to coalesce related notifications.</summary>
        protected string _groupKey;
        /// <summary>The background color override.</summary>
        protected NotificationColor? _backgroundColor;
        /// <summary>The foreground color override.</summary>
        protected NotificationColor? _foregroundColor;
        /// <summary>The text settings applied to the title.</summary>
        protected NotificationTextSettings _titleSettings;
        /// <summary>The text settings applied to the message.</summary>
        protected NotificationTextSettings _messageSettings;
        /// <summary>The text trimming mode.</summary>
        protected NotificationTextTrimType _trimType = NotificationTextTrimType.NoTrim;
        /// <summary>The maximum number of message rows before trimming applies.</summary>
        protected uint _rowsCount = 2;
        /// <summary>The action invoked when the left button is clicked.</summary>
        protected Action _leftButtonAction;
        /// <summary>The content (text) of the left button.</summary>
        protected string _leftButtonContent;
        /// <summary>The action invoked when the right button is clicked.</summary>
        protected Action _rightButtonAction;
        /// <summary>The content (text) of the right button.</summary>
        protected string _rightButtonContent;
        /// <summary>The action invoked when the notification is clicked.</summary>
        protected Action _onClick;
        /// <summary>The action invoked when the notification is closed.</summary>
        protected Action _onClose;
        /// <summary>The notification icon.</summary>
        protected object _icon;
        /// <summary>The platform-specific image object.</summary>
        protected object _platformImage;
        /// <summary>The collection of platform-specific extension values.</summary>
        protected Dictionary<string, object> _extensions;

        /// <summary>
        /// Creates a new empty <see cref="NotificationBuilder"/> instance.
        /// </summary>
        /// <returns>A new builder instance.</returns>
        public static NotificationBuilder Create() => new NotificationBuilder();

        /// <summary>
        /// Creates a new <see cref="NotificationBuilder"/> instance with the specified title and message.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>A new builder instance with title and message set.</returns>
        public static NotificationBuilder Create(string title, string message) =>
            new NotificationBuilder().WithTitle(title).WithMessage(message);

        /// <summary>
        /// Sets the notification title.
        /// </summary>
        /// <param name="title">The title text.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithTitle(string title) { _title = title; return this; }

        /// <summary>
        /// Sets the notification message.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithMessage(string message) { _message = message; return this; }

        /// <summary>
        /// Sets the notification type.
        /// </summary>
        /// <param name="type">The notification type.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder OfType(NotificationType type) { _type = type; return this; }

        /// <summary>
        /// Sets the notification type to <see cref="NotificationType.Information"/>.
        /// </summary>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder AsInformation() => OfType(NotificationType.Information);

        /// <summary>
        /// Sets the notification type to <see cref="NotificationType.Success"/>.
        /// </summary>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder AsSuccess() => OfType(NotificationType.Success);

        /// <summary>
        /// Sets the notification type to <see cref="NotificationType.Warning"/>.
        /// </summary>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder AsWarning() => OfType(NotificationType.Warning);

        /// <summary>
        /// Sets the notification type to <see cref="NotificationType.Error"/>.
        /// </summary>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder AsError() => OfType(NotificationType.Error);

        /// <summary>
        /// Sets the notification area name where the notification will be displayed.
        /// </summary>
        /// <param name="areaName">The target area name.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder InArea(string areaName) { _areaName = areaName; return this; }

        /// <summary>
        /// Sets the notification expiration time.
        /// </summary>
        /// <param name="time">The duration after which the notification automatically closes.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder ExpiresIn(TimeSpan time) { _expirationTime = time; return this; }

        /// <summary>
        /// Sets the notification expiration time in seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds before the notification closes.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder ExpiresInSeconds(double seconds) => ExpiresIn(TimeSpan.FromSeconds(seconds));

        /// <summary>
        /// Configures the notification to never expire automatically.
        /// </summary>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder NeverExpires() { _expirationTime = TimeSpan.MaxValue; return this; }

        /// <summary>
        /// Sets whether the notification closes when clicked.
        /// </summary>
        /// <param name="close">True to close on click; false to keep open.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder CloseOnClick(bool close = true) { _closeOnClick = close; return this; }

        /// <summary>
        /// Hides the close button on the notification.
        /// </summary>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder HideCloseButton() { _showCloseButton = false; return this; }

        /// <summary>
        /// Sets the notification display priority.
        /// </summary>
        /// <param name="priority">The priority level.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithPriority(NotificationPriority priority) { _priority = priority; return this; }

        /// <summary>
        /// Sets the group key for notification deduplication.
        /// </summary>
        /// <param name="groupKey">The group key. Notifications with the same key replace each other.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder GroupAs(string groupKey) { _groupKey = groupKey; return this; }

        /// <summary>
        /// Sets the notification background color.
        /// </summary>
        /// <param name="color">The background color.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithBackground(NotificationColor color) { _backgroundColor = color; return this; }

        /// <summary>
        /// Sets the notification background color from a hex string.
        /// </summary>
        /// <param name="hex">The hex color string (e.g., "#FF0000").</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithBackground(string hex) { _backgroundColor = NotificationColor.FromHex(hex); return this; }

        /// <summary>
        /// Sets the notification foreground (text) color.
        /// </summary>
        /// <param name="color">The foreground color.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithForeground(NotificationColor color) { _foregroundColor = color; return this; }

        /// <summary>
        /// Sets the notification foreground (text) color from a hex string.
        /// </summary>
        /// <param name="hex">The hex color string (e.g., "#FFFFFF").</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithForeground(string hex) { _foregroundColor = NotificationColor.FromHex(hex); return this; }

        /// <summary>
        /// Configures the title text rendering settings.
        /// </summary>
        /// <param name="configure">An action to configure the title text settings.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithTitleSettings(Action<NotificationTextSettings> configure)
        {
            _titleSettings = _titleSettings ?? new NotificationTextSettings();
            configure(_titleSettings);
            return this;
        }

        /// <summary>
        /// Configures the message text rendering settings.
        /// </summary>
        /// <param name="configure">An action to configure the message text settings.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithMessageSettings(Action<NotificationTextSettings> configure)
        {
            _messageSettings = _messageSettings ?? new NotificationTextSettings();
            configure(_messageSettings);
            return this;
        }

        /// <summary>
        /// Configures text trimming behavior for the notification message.
        /// </summary>
        /// <param name="trimType">The text trimming type.</param>
        /// <param name="rows">The maximum number of visible rows before trimming.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithTrimming(NotificationTextTrimType trimType, uint rows = 2)
        {
            _trimType = trimType;
            _rowsCount = rows;
            return this;
        }

        /// <summary>
        /// Configures the left button with text and a click action.
        /// </summary>
        /// <param name="text">The button label text.</param>
        /// <param name="action">The action invoked when the button is clicked.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithLeftButton(string text, Action action)
        {
            _leftButtonContent = text;
            _leftButtonAction = action;
            return this;
        }

        /// <summary>
        /// Configures the right button with text and a click action.
        /// </summary>
        /// <param name="text">The button label text.</param>
        /// <param name="action">The action invoked when the button is clicked.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithRightButton(string text, Action action)
        {
            _rightButtonContent = text;
            _rightButtonAction = action;
            return this;
        }

        /// <summary>
        /// Configures Ok and Cancel buttons with the specified actions.
        /// </summary>
        /// <param name="onOk">The action invoked when Ok is clicked.</param>
        /// <param name="onCancel">The action invoked when Cancel is clicked. If null, a no-op action is used.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithOkCancel(Action onOk, Action onCancel = null) =>
            WithLeftButton("Ok", onOk).WithRightButton("Cancel", onCancel ?? (() => { }));

        /// <summary>
        /// Sets the action invoked when the notification body is clicked.
        /// </summary>
        /// <param name="onClick">The click action.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder OnClick(Action onClick) { _onClick = onClick; return this; }

        /// <summary>
        /// Sets the action invoked when the notification is closed.
        /// </summary>
        /// <param name="onClose">The close action.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder OnClose(Action onClose) { _onClose = onClose; return this; }

        /// <summary>
        /// Sets the notification icon. The type is platform-dependent.
        /// </summary>
        /// <param name="icon">The icon object.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithIcon(object icon) { _icon = icon; return this; }

        /// <summary>
        /// Sets the notification image.
        /// </summary>
        /// <param name="image">The image data to display in the notification.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithImage(NotificationImageData image)
        {
            _platformImage = image;
            return this;
        }

        /// <summary>
        /// Adds a custom extension key-value pair to the notification.
        /// </summary>
        /// <param name="key">The extension key.</param>
        /// <param name="value">The extension value.</param>
        /// <returns>The current builder instance for chaining.</returns>
        public NotificationBuilder WithExtension(string key, object value)
        {
            _extensions = _extensions ?? new Dictionary<string, object>();
            _extensions[key] = value;
            return this;
        }

        /// <summary>
        /// Builds and returns the configured <see cref="NotificationRequest"/> instance.
        /// </summary>
        /// <returns>A new <see cref="NotificationRequest"/> populated with the builder's settings.</returns>
        public NotificationRequest Build() => new NotificationRequest
        {
            Title = _title,
            Message = _message,
            Type = _type,
            AreaName = _areaName,
            ExpirationTime = _expirationTime,
            CloseOnClick = _closeOnClick,
            ShowCloseButton = _showCloseButton,
            Priority = _priority,
            GroupKey = _groupKey,
            BackgroundColor = _backgroundColor,
            ForegroundColor = _foregroundColor,
            TitleSettings = _titleSettings,
            MessageSettings = _messageSettings,
            TrimType = _trimType,
            RowsCount = _rowsCount,
            LeftButtonAction = _leftButtonAction,
            LeftButtonContent = _leftButtonContent,
            RightButtonAction = _rightButtonAction,
            RightButtonContent = _rightButtonContent,
            OnClick = _onClick,
            OnClose = _onClose,
            Icon = _icon,
            PlatformImage = _platformImage,
            Extensions = _extensions
        };
    }
}
