using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Notification.Wpf.Converters
{
    [ValueConversion(typeof(Brush), typeof(double)), MarkupExtensionReturnType(typeof(ColorAlphaConverter))]
    internal class ColorAlphaConverter : ValueConverter
    {
        /// <inheritdoc />
        public override object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is Brush convertBrush)
            {
                var copy = convertBrush.Clone();
                copy.Opacity = 0.2;
                return copy;
            }
            return v;
        }

        public override object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }
}

