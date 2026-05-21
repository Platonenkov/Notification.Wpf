namespace Notification.Core
{
    /// <summary>
    /// Represents text rendering settings for notification content, including font, alignment, and opacity.
    /// </summary>
    public class NotificationTextSettings
    {
        /// <summary>Gets or sets the font family name. Defaults to "Segoe UI".</summary>
        public string FontFamily { get; set; } = "Segoe UI";

        /// <summary>Gets or sets the font size in device-independent pixels. Defaults to 14.0.</summary>
        public double FontSize { get; set; } = 14.0;

        /// <summary>Gets or sets the font style (e.g., Normal, Italic). Defaults to <see cref="NotificationFontStyle.Normal"/>.</summary>
        public NotificationFontStyle FontStyle { get; set; } = NotificationFontStyle.Normal;

        /// <summary>Gets or sets the font weight (e.g., Normal, Bold). Defaults to <see cref="NotificationFontWeight.Normal"/>.</summary>
        public NotificationFontWeight FontWeight { get; set; } = NotificationFontWeight.Normal;

        /// <summary>Gets or sets the text alignment within the text block. Defaults to <see cref="NotificationTextAlignment.Left"/>.</summary>
        public NotificationTextAlignment TextAlignment { get; set; } = NotificationTextAlignment.Left;

        /// <summary>Gets or sets the horizontal alignment of the text element. Defaults to <see cref="NotificationHorizontalAlignment.Stretch"/>.</summary>
        public NotificationHorizontalAlignment HorizontalAlignment { get; set; } = NotificationHorizontalAlignment.Stretch;

        /// <summary>Gets or sets the vertical alignment of the text element. Defaults to <see cref="NotificationVerticalAlignment.Stretch"/>.</summary>
        public NotificationVerticalAlignment VerticalAlignment { get; set; } = NotificationVerticalAlignment.Stretch;

        /// <summary>Gets or sets the text opacity, where 0.0 is fully transparent and 1.0 is fully opaque. Defaults to 1.0.</summary>
        public double Opacity { get; set; } = 1.0;
    }
}
