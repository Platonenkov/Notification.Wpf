using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Text;
using System.Windows.Media;

using Notification.Wpf.Base;
using Notification.Wpf.Base.Options;
using Notification.Wpf.Constants;

namespace Notification.Wpf
{
    /// <inheritdoc cref="ICustomizedNotification" />
    public class BaseNotificationContent : CustomizedOptions, ICustomizedNotification, INotifyPropertyChanged
    {
        private string _title;
        private string _message;

        /// <summary>属性值更改时发生。</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>引发 <see cref="PropertyChanged"/> 事件。</summary>
        /// <param name="propertyName">已更改的属性名。</param>
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <inheritdoc />
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        /// <inheritdoc />
        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(nameof(Message)); }
        }

    }
}