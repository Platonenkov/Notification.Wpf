using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Notification.Wpf
{
    public class NotificationTemplateSelector : DataTemplateSelector
    {
        private DataTemplate _defaultStringTemplate;
        private DataTemplate _defaultNotificationTemplate;
        private DataTemplate _defaultImageSourceTemplate;

        private void GetTemplatesFromResources(FrameworkElement container)
        {
            _defaultStringTemplate =
                    container?.FindResource("DefaultStringTemplate") as DataTemplate;
            _defaultNotificationTemplate =
                    container?.FindResource("DefaultNotificationTemplate") as DataTemplate;
            _defaultImageSourceTemplate =
                    container?.FindResource("DefaultImageSourceTemplate") as DataTemplate;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (_defaultStringTemplate == null && _defaultNotificationTemplate == null)
            {
                GetTemplatesFromResources((FrameworkElement)container);                            
            }

            return item switch
            {
                string => _defaultStringTemplate,
                ImageSource => _defaultImageSourceTemplate,
                NotificationContent => _defaultNotificationTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}