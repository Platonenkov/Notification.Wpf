using Notification.Wpf.Base.Interfaces.Options;
using Notification.Wpf.Classes;

namespace Notification.Wpf
{
    /// <summary>
    /// Notification template
    /// </summary>
    public interface INotification : ICustomizedNotification, IButtonOptions
    {
        /// <summary> Image </summary>
        public NotificationImage Image { get; set; }

        /// <summary> Notification type (change color) </summary>
        NotificationType Type { get; set; }

        /// <summary> close message when OnClick to message window </summary>
        public bool CloseOnClick { get; set; }

    }
}