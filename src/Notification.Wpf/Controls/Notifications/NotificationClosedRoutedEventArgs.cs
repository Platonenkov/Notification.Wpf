using System.Windows;
using Notification.Core;

namespace Notification.Wpf.Controls
{
    /// <summary>
    /// Routed event args raised by <see cref="Notification.NotificationClosed"/> carrying
    /// the close reason and the action button information (when applicable).
    /// </summary>
    public sealed class NotificationClosedRoutedEventArgs : RoutedEventArgs
    {
        /// <summary>Why the notification was closed.</summary>
        public NotificationCloseReason Reason { get; }

        /// <summary>The action button id when <see cref="Reason"/> is <see cref="NotificationCloseReason.ActionButton"/>, otherwise null.</summary>
        public string ButtonId { get; }

        /// <summary>The zero-based action button index when applicable.</summary>
        public int? ButtonIndex { get; }

        public NotificationClosedRoutedEventArgs(
            RoutedEvent routedEvent,
            object source,
            NotificationCloseReason reason,
            string buttonId,
            int? buttonIndex)
            : base(routedEvent, source)
        {
            Reason = reason;
            ButtonId = buttonId;
            ButtonIndex = buttonIndex;
        }
    }
}
