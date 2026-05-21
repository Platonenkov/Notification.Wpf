using System.Windows;
using System.Windows.Media;
using Notification.Core;
using Notification.Wpf.Base;

namespace Notification.Wpf.Adapters
{
    /// <summary>
    /// Provides extension methods to convert text settings between the framework-agnostic
    /// Core model (<see cref="NotificationTextSettings"/>) and the WPF model (<see cref="TextContentSettings"/>).
    /// </summary>
    public static class TextSettingsExtensions
    {
        /// <summary>
        /// Converts framework-agnostic Core text settings into WPF text content settings.
        /// </summary>
        /// <param name="settings">The Core text settings to convert; may be <see langword="null"/>.</param>
        /// <returns>
        /// A <see cref="TextContentSettings"/> instance with equivalent WPF values,
        /// or <see langword="null"/> if <paramref name="settings"/> is <see langword="null"/>.
        /// </returns>
        public static TextContentSettings ToWpfSettings(this NotificationTextSettings settings)
        {
            if (settings == null)
                return null;

            return new TextContentSettings
            {
                FontFamily = new FontFamily(settings.FontFamily),
                FontSize = settings.FontSize,
                FontStyle = settings.FontStyle.ToWpfFontStyle(),
                FontWeight = settings.FontWeight.ToWpfFontWeight(),
                TextAlignment = settings.TextAlignment.ToWpfTextAlignment(),
                HorizontalAlignment = settings.HorizontalAlignment.ToWpfHorizontalAlignment(),
                VerticalTextAlignment = settings.VerticalAlignment.ToWpfVerticalAlignment(),
                Opacity = settings.Opacity
            };
        }

        /// <summary>
        /// Converts WPF text content settings into framework-agnostic Core text settings.
        /// </summary>
        /// <param name="settings">The WPF text content settings to convert; may be <see langword="null"/>.</param>
        /// <returns>
        /// A <see cref="NotificationTextSettings"/> instance with equivalent Core values,
        /// or <see langword="null"/> if <paramref name="settings"/> is <see langword="null"/>.
        /// The font family defaults to "Segoe UI" when not specified.
        /// </returns>
        public static NotificationTextSettings ToCoreSettings(this TextContentSettings settings)
        {
            if (settings == null)
                return null;

            return new NotificationTextSettings
            {
                FontFamily = settings.FontFamily?.Source ?? "Segoe UI",
                FontSize = settings.FontSize,
                FontStyle = settings.FontStyle.ToCoreStyle(),
                FontWeight = settings.FontWeight.ToCoreWeight(),
                TextAlignment = settings.TextAlignment.ToCoreAlignment(),
                HorizontalAlignment = settings.HorizontalAlignment.ToCoreHorizontalAlignment(),
                VerticalAlignment = settings.VerticalTextAlignment.ToCoreVerticalAlignment(),
                Opacity = settings.Opacity
            };
        }

        /// <summary>
        /// Converts a Core <see cref="NotificationFontStyle"/> value into the corresponding WPF <see cref="FontStyle"/>.
        /// </summary>
        /// <param name="style">The Core font style to convert.</param>
        /// <returns>The matching WPF <see cref="FontStyle"/>; defaults to <see cref="FontStyles.Normal"/>.</returns>
        public static FontStyle ToWpfFontStyle(this NotificationFontStyle style) => style switch
        {
            NotificationFontStyle.Italic => FontStyles.Italic,
            NotificationFontStyle.Oblique => FontStyles.Oblique,
            _ => FontStyles.Normal
        };

        /// <summary>
        /// Converts a WPF <see cref="FontStyle"/> value into the corresponding Core <see cref="NotificationFontStyle"/>.
        /// </summary>
        /// <param name="style">The WPF font style to convert.</param>
        /// <returns>The matching Core <see cref="NotificationFontStyle"/>; defaults to <see cref="NotificationFontStyle.Normal"/>.</returns>
        public static NotificationFontStyle ToCoreStyle(this FontStyle style)
        {
            if (style == FontStyles.Italic) return NotificationFontStyle.Italic;
            if (style == FontStyles.Oblique) return NotificationFontStyle.Oblique;
            return NotificationFontStyle.Normal;
        }

        /// <summary>
        /// Converts a Core <see cref="NotificationFontWeight"/> value into a WPF <see cref="FontWeight"/>
        /// using its OpenType numeric weight.
        /// </summary>
        /// <param name="weight">The Core font weight to convert.</param>
        /// <returns>The matching WPF <see cref="FontWeight"/>.</returns>
        public static FontWeight ToWpfFontWeight(this NotificationFontWeight weight) =>
            FontWeight.FromOpenTypeWeight((int)weight);

        /// <summary>
        /// Converts a WPF <see cref="FontWeight"/> value into a Core <see cref="NotificationFontWeight"/>
        /// using its OpenType numeric weight.
        /// </summary>
        /// <param name="weight">The WPF font weight to convert.</param>
        /// <returns>The matching Core <see cref="NotificationFontWeight"/>.</returns>
        public static NotificationFontWeight ToCoreWeight(this FontWeight weight) =>
            (NotificationFontWeight)weight.ToOpenTypeWeight();

        /// <summary>
        /// Converts a Core <see cref="NotificationTextAlignment"/> value into the corresponding WPF <see cref="TextAlignment"/>.
        /// </summary>
        /// <param name="alignment">The Core text alignment to convert.</param>
        /// <returns>The matching WPF <see cref="TextAlignment"/>; defaults to <see cref="TextAlignment.Left"/>.</returns>
        public static TextAlignment ToWpfTextAlignment(this NotificationTextAlignment alignment) => alignment switch
        {
            NotificationTextAlignment.Center => TextAlignment.Center,
            NotificationTextAlignment.Right => TextAlignment.Right,
            NotificationTextAlignment.Justify => TextAlignment.Justify,
            _ => TextAlignment.Left
        };

        /// <summary>
        /// Converts a WPF <see cref="TextAlignment"/> value into the corresponding Core <see cref="NotificationTextAlignment"/>.
        /// </summary>
        /// <param name="alignment">The WPF text alignment to convert.</param>
        /// <returns>The matching Core <see cref="NotificationTextAlignment"/>; defaults to <see cref="NotificationTextAlignment.Left"/>.</returns>
        public static NotificationTextAlignment ToCoreAlignment(this TextAlignment alignment) => alignment switch
        {
            TextAlignment.Center => NotificationTextAlignment.Center,
            TextAlignment.Right => NotificationTextAlignment.Right,
            TextAlignment.Justify => NotificationTextAlignment.Justify,
            _ => NotificationTextAlignment.Left
        };

        /// <summary>
        /// Converts a Core <see cref="NotificationHorizontalAlignment"/> value into the corresponding WPF <see cref="HorizontalAlignment"/>.
        /// </summary>
        /// <param name="alignment">The Core horizontal alignment to convert.</param>
        /// <returns>The matching WPF <see cref="HorizontalAlignment"/>; defaults to <see cref="HorizontalAlignment.Stretch"/>.</returns>
        public static HorizontalAlignment ToWpfHorizontalAlignment(this NotificationHorizontalAlignment alignment) => alignment switch
        {
            NotificationHorizontalAlignment.Left => HorizontalAlignment.Left,
            NotificationHorizontalAlignment.Center => HorizontalAlignment.Center,
            NotificationHorizontalAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Stretch
        };

        /// <summary>
        /// Converts a WPF <see cref="HorizontalAlignment"/> value into the corresponding Core <see cref="NotificationHorizontalAlignment"/>.
        /// </summary>
        /// <param name="alignment">The WPF horizontal alignment to convert.</param>
        /// <returns>The matching Core <see cref="NotificationHorizontalAlignment"/>; defaults to <see cref="NotificationHorizontalAlignment.Stretch"/>.</returns>
        public static NotificationHorizontalAlignment ToCoreHorizontalAlignment(this HorizontalAlignment alignment) => alignment switch
        {
            HorizontalAlignment.Left => NotificationHorizontalAlignment.Left,
            HorizontalAlignment.Center => NotificationHorizontalAlignment.Center,
            HorizontalAlignment.Right => NotificationHorizontalAlignment.Right,
            _ => NotificationHorizontalAlignment.Stretch
        };

        /// <summary>
        /// Converts a Core <see cref="NotificationVerticalAlignment"/> value into the corresponding WPF <see cref="VerticalAlignment"/>.
        /// </summary>
        /// <param name="alignment">The Core vertical alignment to convert.</param>
        /// <returns>The matching WPF <see cref="VerticalAlignment"/>; defaults to <see cref="VerticalAlignment.Stretch"/>.</returns>
        public static VerticalAlignment ToWpfVerticalAlignment(this NotificationVerticalAlignment alignment) => alignment switch
        {
            NotificationVerticalAlignment.Top => VerticalAlignment.Top,
            NotificationVerticalAlignment.Center => VerticalAlignment.Center,
            NotificationVerticalAlignment.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Stretch
        };

        /// <summary>
        /// Converts a WPF <see cref="VerticalAlignment"/> value into the corresponding Core <see cref="NotificationVerticalAlignment"/>.
        /// </summary>
        /// <param name="alignment">The WPF vertical alignment to convert.</param>
        /// <returns>The matching Core <see cref="NotificationVerticalAlignment"/>; defaults to <see cref="NotificationVerticalAlignment.Stretch"/>.</returns>
        public static NotificationVerticalAlignment ToCoreVerticalAlignment(this VerticalAlignment alignment) => alignment switch
        {
            VerticalAlignment.Top => NotificationVerticalAlignment.Top,
            VerticalAlignment.Center => NotificationVerticalAlignment.Center,
            VerticalAlignment.Bottom => NotificationVerticalAlignment.Bottom,
            _ => NotificationVerticalAlignment.Stretch
        };
    }
}
