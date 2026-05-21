using System.Windows.Media;
using Notification.Core;

namespace Notification.Wpf.Adapters
{
    /// <summary>
    /// Provides extension methods to convert colors between the framework-agnostic Core model
    /// (<see cref="NotificationColor"/>) and WPF brushes (<see cref="Brush"/>).
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Creates a WPF <see cref="SolidColorBrush"/> from a Core <see cref="NotificationColor"/>.
        /// </summary>
        /// <param name="color">The Core color to convert, including its alpha channel.</param>
        /// <returns>A <see cref="SolidColorBrush"/> with the equivalent ARGB color.</returns>
        public static SolidColorBrush ToBrush(this NotificationColor color) =>
            new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));

        /// <summary>
        /// Converts a WPF <see cref="SolidColorBrush"/> into a Core <see cref="NotificationColor"/>.
        /// </summary>
        /// <param name="brush">The solid color brush to convert.</param>
        /// <returns>A <see cref="NotificationColor"/> with the brush's RGBA components.</returns>
        public static NotificationColor ToNotificationColor(this SolidColorBrush brush) =>
            new NotificationColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A);

        /// <summary>
        /// Converts a WPF <see cref="Brush"/> into a Core <see cref="NotificationColor"/>.
        /// </summary>
        /// <param name="brush">The brush to convert.</param>
        /// <returns>
        /// A <see cref="NotificationColor"/> with the RGBA components of the brush when it is a
        /// <see cref="SolidColorBrush"/>; otherwise <see cref="NotificationColor.DarkGray"/>.
        /// </returns>
        public static NotificationColor ToNotificationColor(this Brush brush)
        {
            if (brush is SolidColorBrush scb)
                return new NotificationColor(scb.Color.R, scb.Color.G, scb.Color.B, scb.Color.A);
            return NotificationColor.DarkGray;
        }
    }
}
