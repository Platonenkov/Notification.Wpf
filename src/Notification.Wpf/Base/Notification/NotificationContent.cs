using System;
using Notification.Wpf.Base.Interfaces.Options;
using Notification.Wpf.Classes;
using Notification.Wpf.Constants;

namespace Notification.Wpf
{
    /// <summary> Message </summary>
    public class NotificationContent : BaseNotificationContent, INotification
    {
        ///<inheritdoc/>
        public NotificationImage Image { get; set; }

        ///<inheritdoc/>
        public NotificationType Type { get; set; }

        #region Left button

        private object _LeftButtonContent = NotificationConstants.DefaultLeftButtonContent;

        ///<inheritdoc/>
        public object LeftButtonContent
        {
            get => _LeftButtonContent;
            set
            {
                switch (value)
                {
                    case string button_name when string.IsNullOrWhiteSpace(button_name):
                    case null:
                        _LeftButtonContent = NotificationConstants.DefaultLeftButtonContent;
                        break;
                    default:
                        _LeftButtonContent = value;
                        break;
                }
            }
        }

        ///<inheritdoc/>
        public Action LeftButtonAction { get; set; }

        #endregion

        #region RightButton

        private object _RightButtonContent = NotificationConstants.DefaultRightButtonContent;

        ///<inheritdoc/>
        public object RightButtonContent
        {
            get => _RightButtonContent;
            set
            {
                switch (value)
                {
                    case string button_name when string.IsNullOrWhiteSpace(button_name):
                    case null:
                        _RightButtonContent = NotificationConstants.DefaultRightButtonContent;
                        break;
                    default:
                        _RightButtonContent = value;
                        break;
                }
            }
        }

        ///<inheritdoc/>
        public Action RightButtonAction { get; set; }

        #endregion

        ///<inheritdoc/>
        public bool CloseOnClick { get; set; } = true;
        /// <summary> Get valid options after check for null </summary>
        /// <param name="CloseOnClick"></param>
        /// <param name="options">options</param>
        /// <param name="Title"></param>
        /// <param name="Message"></param>
        /// <param name="type"></param>
        /// <param name="LeftButtonAction"></param>
        /// <param name="LeftButtonContent"></param>
        /// <param name="RightButtonAction"></param>
        /// <param name="RightButtonContent"></param>
        /// <param name="Image"></param>
        /// <returns></returns>
        public static NotificationContent GetValidContent(string Title, string Message, NotificationType type = NotificationType.None,
            Action LeftButtonAction = null, object LeftButtonContent = null,
            Action RightButtonAction = null, object RightButtonContent = null,
            NotificationImage Image = null, bool CloseOnClick = true,
            ICustomizedOptions options = null) => new()
            {
                Title = Title,
                Message = Message,


                Type = type,
                Image = Image,
                CloseOnClick = CloseOnClick,


                LeftButtonAction = LeftButtonAction,
                LeftButtonContent = LeftButtonContent ?? NotificationConstants.DefaultLeftButtonContent,
                RightButtonAction = RightButtonAction,
                RightButtonContent = RightButtonContent ?? NotificationConstants.DefaultRightButtonContent,


                Background = options?.Background ?? NotificationConstants.DefaultBackgroundColor,
                Foreground = options?.Foreground ?? NotificationConstants.DefaultForegroundColor,
                Icon = options?.Icon,
                MessageTextSettings = options?.MessageTextSettings,
                TitleTextSettings = options?.TitleTextSettings,
                RowsCount = options?.RowsCount is { } count and > 0 ? count : 1,
                TrimType = options?.TrimType ?? NotificationConstants.DefaulTextTrimType
            };


    }
}
