using Notification.Core;
using Notification.Wpf.Base.Interfaces.Base;

namespace Notification.Wpf
{
    /// <summary> Notification manager for popup messages </summary>
    public interface INotificationManager : INotificationService, IMessageManager, IProgressManager, IButtonMessageManager
    {
    }
}