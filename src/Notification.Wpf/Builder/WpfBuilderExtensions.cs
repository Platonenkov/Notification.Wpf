using System.Windows.Media;
using Notification.Core;
using Notification.Wpf.Base;
using Notification.Wpf.Classes;

namespace Notification.Wpf.Builder
{
    public static class WpfBuilderExtensions
    {
        public static NotificationBuilder WithBackground(this NotificationBuilder builder, Brush brush) =>
            builder.WithExtension("Wpf.Background", brush);

        public static NotificationBuilder WithForeground(this NotificationBuilder builder, Brush brush) =>
            builder.WithExtension("Wpf.Foreground", brush);

        public static NotificationBuilder WithImage(this NotificationBuilder builder, ImageSource source, ImagePosition position = ImagePosition.Top) =>
            builder.WithExtension("Wpf.Image", new NotificationImage { Source = source, Position = position });

        public static NotificationBuilder WithWpfTitleSettings(this NotificationBuilder builder, TextContentSettings settings) =>
            builder.WithExtension("Wpf.TitleSettings", settings);

        public static NotificationBuilder WithWpfMessageSettings(this NotificationBuilder builder, TextContentSettings settings) =>
            builder.WithExtension("Wpf.MessageSettings", settings);
    }
}
