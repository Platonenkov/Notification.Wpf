using System.Windows.Media;
using Notification.Core;
using Notification.Wpf.Base;
using Notification.Wpf.Classes;

namespace Notification.Wpf.Builder
{
    /// <summary>
    /// Provides WPF-specific fluent extension methods for <see cref="NotificationBuilder"/>,
    /// attaching WPF visual data (brushes, images, text settings) as builder extensions.
    /// </summary>
    public static class WpfBuilderExtensions
    {
        /// <summary>
        /// Attaches a WPF background brush to the notification being built.
        /// </summary>
        /// <param name="builder">The notification builder to extend.</param>
        /// <param name="brush">The WPF <see cref="Brush"/> to use as the notification background.</param>
        /// <returns>The same <paramref name="builder"/> instance for fluent chaining.</returns>
        public static NotificationBuilder WithBackground(this NotificationBuilder builder, Brush brush) =>
            builder.WithExtension("Wpf.Background", brush);

        /// <summary>
        /// Attaches a WPF foreground brush to the notification being built.
        /// </summary>
        /// <param name="builder">The notification builder to extend.</param>
        /// <param name="brush">The WPF <see cref="Brush"/> to use as the notification foreground.</param>
        /// <returns>The same <paramref name="builder"/> instance for fluent chaining.</returns>
        public static NotificationBuilder WithForeground(this NotificationBuilder builder, Brush brush) =>
            builder.WithExtension("Wpf.Foreground", brush);

        /// <summary>
        /// Attaches a WPF image to the notification being built.
        /// </summary>
        /// <param name="builder">The notification builder to extend.</param>
        /// <param name="source">The WPF <see cref="ImageSource"/> to display.</param>
        /// <param name="position">The position of the image within the notification. Defaults to <see cref="ImagePosition.Top"/>.</param>
        /// <returns>The same <paramref name="builder"/> instance for fluent chaining.</returns>
        public static NotificationBuilder WithImage(this NotificationBuilder builder, ImageSource source, ImagePosition position = ImagePosition.Top) =>
            builder.WithExtension("Wpf.Image", new NotificationImage { Source = source, Position = position });

        /// <summary>
        /// Attaches WPF text settings for the notification title to the builder.
        /// </summary>
        /// <param name="builder">The notification builder to extend.</param>
        /// <param name="settings">The WPF <see cref="TextContentSettings"/> to apply to the title.</param>
        /// <returns>The same <paramref name="builder"/> instance for fluent chaining.</returns>
        public static NotificationBuilder WithWpfTitleSettings(this NotificationBuilder builder, TextContentSettings settings) =>
            builder.WithExtension("Wpf.TitleSettings", settings);

        /// <summary>
        /// Attaches WPF text settings for the notification message to the builder.
        /// </summary>
        /// <param name="builder">The notification builder to extend.</param>
        /// <param name="settings">The WPF <see cref="TextContentSettings"/> to apply to the message.</param>
        /// <returns>The same <paramref name="builder"/> instance for fluent chaining.</returns>
        public static NotificationBuilder WithWpfMessageSettings(this NotificationBuilder builder, TextContentSettings settings) =>
            builder.WithExtension("Wpf.MessageSettings", settings);
    }
}
