using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Notification.Wpf.Converters;
using Notifications.Wpf.ViewModels;

namespace Notification.Wpf
{
    [ValueConversion(typeof(NotificationViewModel), typeof(Visibility)), MarkupExtensionReturnType(typeof(NotificationAttachVisibleConverter))]
    internal class NotificationAttachVisibleConverter : ValueConverter
    {
        public override object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (!(v is NotificationViewModel content)) return Visibility.Collapsed;
            return (content.Message?.Length ?? 0) < 43 * content.RowsCount ? Visibility.Collapsed : Visibility.Visible;

        }
    }
}