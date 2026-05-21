using System.Windows;
using System.Windows.Automation.Text;
using System.Windows.Media;

using Notification.Wpf.Base;
using Notification.Wpf.Base.Options;
using Notification.Wpf.Constants;

namespace Notification.Wpf
{
    /// <inheritdoc cref="ICustomizedNotification" />
    public class BaseNotificationContent : CustomizedOptions, ICustomizedNotification
    {
        /// <inheritdoc />
        public string Title { get; set; }
        /// <inheritdoc />
        public string Message { get; set; }

    }
}