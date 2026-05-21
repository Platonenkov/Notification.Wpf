using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Notification.Wpf
{
    /// <summary>
    /// Selects the appropriate <see cref="DataTemplate"/> for a notification item based on its content type.
    /// </summary>
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

        /// <summary>
        /// Returns the <see cref="DataTemplate"/> matching the type of the supplied item.
        /// </summary>
        /// <param name="item">The data object for which to select the template.</param>
        /// <param name="container">The element bound to the data object.</param>
        /// <returns>The <see cref="DataTemplate"/> that matches the item type, or the base template if none matches.</returns>
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