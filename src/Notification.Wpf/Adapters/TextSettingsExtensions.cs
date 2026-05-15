using System.Windows;
using System.Windows.Media;
using Notification.Core;
using Notification.Wpf.Base;

namespace Notification.Wpf.Adapters
{
    public static class TextSettingsExtensions
    {
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

        public static FontStyle ToWpfFontStyle(this NotificationFontStyle style) => style switch
        {
            NotificationFontStyle.Italic => FontStyles.Italic,
            NotificationFontStyle.Oblique => FontStyles.Oblique,
            _ => FontStyles.Normal
        };

        public static NotificationFontStyle ToCoreStyle(this FontStyle style)
        {
            if (style == FontStyles.Italic) return NotificationFontStyle.Italic;
            if (style == FontStyles.Oblique) return NotificationFontStyle.Oblique;
            return NotificationFontStyle.Normal;
        }

        public static FontWeight ToWpfFontWeight(this NotificationFontWeight weight) =>
            FontWeight.FromOpenTypeWeight((int)weight);

        public static NotificationFontWeight ToCoreWeight(this FontWeight weight) =>
            (NotificationFontWeight)weight.ToOpenTypeWeight();

        public static TextAlignment ToWpfTextAlignment(this NotificationTextAlignment alignment) => alignment switch
        {
            NotificationTextAlignment.Center => TextAlignment.Center,
            NotificationTextAlignment.Right => TextAlignment.Right,
            NotificationTextAlignment.Justify => TextAlignment.Justify,
            _ => TextAlignment.Left
        };

        public static NotificationTextAlignment ToCoreAlignment(this TextAlignment alignment) => alignment switch
        {
            TextAlignment.Center => NotificationTextAlignment.Center,
            TextAlignment.Right => NotificationTextAlignment.Right,
            TextAlignment.Justify => NotificationTextAlignment.Justify,
            _ => NotificationTextAlignment.Left
        };

        public static HorizontalAlignment ToWpfHorizontalAlignment(this NotificationHorizontalAlignment alignment) => alignment switch
        {
            NotificationHorizontalAlignment.Left => HorizontalAlignment.Left,
            NotificationHorizontalAlignment.Center => HorizontalAlignment.Center,
            NotificationHorizontalAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Stretch
        };

        public static NotificationHorizontalAlignment ToCoreHorizontalAlignment(this HorizontalAlignment alignment) => alignment switch
        {
            HorizontalAlignment.Left => NotificationHorizontalAlignment.Left,
            HorizontalAlignment.Center => NotificationHorizontalAlignment.Center,
            HorizontalAlignment.Right => NotificationHorizontalAlignment.Right,
            _ => NotificationHorizontalAlignment.Stretch
        };

        public static VerticalAlignment ToWpfVerticalAlignment(this NotificationVerticalAlignment alignment) => alignment switch
        {
            NotificationVerticalAlignment.Top => VerticalAlignment.Top,
            NotificationVerticalAlignment.Center => VerticalAlignment.Center,
            NotificationVerticalAlignment.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Stretch
        };

        public static NotificationVerticalAlignment ToCoreVerticalAlignment(this VerticalAlignment alignment) => alignment switch
        {
            VerticalAlignment.Top => NotificationVerticalAlignment.Top,
            VerticalAlignment.Center => NotificationVerticalAlignment.Center,
            VerticalAlignment.Bottom => NotificationVerticalAlignment.Bottom,
            _ => NotificationVerticalAlignment.Stretch
        };
    }
}
