using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Notification.Wpf.Converters
{
    /// <summary>
    /// Base class for value converters that can also be used directly as a XAML markup extension.
    /// </summary>
    [MarkupExtensionReturnType(typeof(ValueConverter))]
    public abstract class ValueConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Returns the converter instance itself as the markup extension value.
        /// </summary>
        /// <param name="sp">The service provider for the markup extension.</param>
        /// <returns>The current converter instance.</returns>
        public override object ProvideValue(IServiceProvider sp) => this;

        /// <summary>
        /// Converts a value from the source to the target.
        /// </summary>
        /// <param name="v">The value produced by the binding source.</param>
        /// <param name="t">The type of the binding target property.</param>
        /// <param name="p">The converter parameter to use.</param>
        /// <param name="c">The culture to use in the converter.</param>
        /// <returns>The converted value.</returns>
        public abstract object Convert(object v, Type t, object p, CultureInfo c);

        /// <summary>
        /// Converts a value from the target back to the source.
        /// </summary>
        /// <param name="v">The value produced by the binding target.</param>
        /// <param name="t">The type to convert to.</param>
        /// <param name="p">The converter parameter to use.</param>
        /// <param name="c">The culture to use in the converter.</param>
        /// <returns>The converted value.</returns>
        public virtual object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }
}
