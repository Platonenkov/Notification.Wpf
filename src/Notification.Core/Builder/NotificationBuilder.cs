using System;
using System.Collections.Generic;

namespace Notification.Core
{
    public class NotificationBuilder
    {
        protected string _title;
        protected string _message;
        protected NotificationType _type = NotificationType.None;
        protected string _areaName = "";
        protected TimeSpan? _expirationTime;
        protected bool _closeOnClick = true;
        protected bool _showCloseButton = true;
        protected NotificationPriority _priority = NotificationPriority.Normal;
        protected string _groupKey;
        protected NotificationColor? _backgroundColor;
        protected NotificationColor? _foregroundColor;
        protected NotificationTextSettings _titleSettings;
        protected NotificationTextSettings _messageSettings;
        protected NotificationTextTrimType _trimType = NotificationTextTrimType.NoTrim;
        protected uint _rowsCount = 2;
        protected Action _leftButtonAction;
        protected string _leftButtonContent;
        protected Action _rightButtonAction;
        protected string _rightButtonContent;
        protected Action _onClick;
        protected Action _onClose;
        protected object _icon;
        protected object _platformImage;
        protected Dictionary<string, object> _extensions;

        public static NotificationBuilder Create() => new NotificationBuilder();

        public static NotificationBuilder Create(string title, string message) =>
            new NotificationBuilder().WithTitle(title).WithMessage(message);

        public NotificationBuilder WithTitle(string title) { _title = title; return this; }
        public NotificationBuilder WithMessage(string message) { _message = message; return this; }
        public NotificationBuilder OfType(NotificationType type) { _type = type; return this; }

        public NotificationBuilder AsInformation() => OfType(NotificationType.Information);
        public NotificationBuilder AsSuccess() => OfType(NotificationType.Success);
        public NotificationBuilder AsWarning() => OfType(NotificationType.Warning);
        public NotificationBuilder AsError() => OfType(NotificationType.Error);

        public NotificationBuilder InArea(string areaName) { _areaName = areaName; return this; }
        public NotificationBuilder ExpiresIn(TimeSpan time) { _expirationTime = time; return this; }
        public NotificationBuilder ExpiresInSeconds(double seconds) => ExpiresIn(TimeSpan.FromSeconds(seconds));
        public NotificationBuilder NeverExpires() { _expirationTime = TimeSpan.MaxValue; return this; }
        public NotificationBuilder CloseOnClick(bool close = true) { _closeOnClick = close; return this; }
        public NotificationBuilder HideCloseButton() { _showCloseButton = false; return this; }

        public NotificationBuilder WithPriority(NotificationPriority priority) { _priority = priority; return this; }
        public NotificationBuilder GroupAs(string groupKey) { _groupKey = groupKey; return this; }

        public NotificationBuilder WithBackground(NotificationColor color) { _backgroundColor = color; return this; }
        public NotificationBuilder WithBackground(string hex) { _backgroundColor = NotificationColor.FromHex(hex); return this; }
        public NotificationBuilder WithForeground(NotificationColor color) { _foregroundColor = color; return this; }
        public NotificationBuilder WithForeground(string hex) { _foregroundColor = NotificationColor.FromHex(hex); return this; }

        public NotificationBuilder WithTitleSettings(Action<NotificationTextSettings> configure)
        {
            _titleSettings = _titleSettings ?? new NotificationTextSettings();
            configure(_titleSettings);
            return this;
        }

        public NotificationBuilder WithMessageSettings(Action<NotificationTextSettings> configure)
        {
            _messageSettings = _messageSettings ?? new NotificationTextSettings();
            configure(_messageSettings);
            return this;
        }

        public NotificationBuilder WithTrimming(NotificationTextTrimType trimType, uint rows = 2)
        {
            _trimType = trimType;
            _rowsCount = rows;
            return this;
        }

        public NotificationBuilder WithLeftButton(string text, Action action)
        {
            _leftButtonContent = text;
            _leftButtonAction = action;
            return this;
        }

        public NotificationBuilder WithRightButton(string text, Action action)
        {
            _rightButtonContent = text;
            _rightButtonAction = action;
            return this;
        }

        public NotificationBuilder WithOkCancel(Action onOk, Action onCancel = null) =>
            WithLeftButton("Ok", onOk).WithRightButton("Cancel", onCancel ?? (() => { }));

        public NotificationBuilder OnClick(Action onClick) { _onClick = onClick; return this; }
        public NotificationBuilder OnClose(Action onClose) { _onClose = onClose; return this; }

        public NotificationBuilder WithIcon(object icon) { _icon = icon; return this; }

        public NotificationBuilder WithImage(NotificationImageData image)
        {
            _platformImage = image;
            return this;
        }

        public NotificationBuilder WithExtension(string key, object value)
        {
            _extensions = _extensions ?? new Dictionary<string, object>();
            _extensions[key] = value;
            return this;
        }

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
