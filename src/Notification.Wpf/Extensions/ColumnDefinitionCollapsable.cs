using System.Windows;
using System.Windows.Controls;

namespace Notifications.Wpf.Extensions
{
    /// <summary>
    /// A <see cref="ColumnDefinition"/> that can be collapsed to zero width via the <see cref="Visible"/> property.
    /// </summary>
    public class ColumnDefinitionCollapsable : ColumnDefinition
    {
        static ColumnDefinitionCollapsable()
        {
            WidthProperty.OverrideMetadata(
                typeof(ColumnDefinitionCollapsable),
                new FrameworkPropertyMetadata(
                    new GridLength(1, GridUnitType.Star),
                    null,
                    (d, v) => ((ColumnDefinitionCollapsable)d).Visible ? v : new GridLength(0)));

            MinWidthProperty.OverrideMetadata(
                typeof(ColumnDefinitionCollapsable),
                new FrameworkPropertyMetadata(0d, null, (d, v) => ((ColumnDefinitionCollapsable)d).Visible ? v : 0d));
        }

        #region Visible : bool - Видимость

        /// <summary>Identifies the <see cref="Visible"/> dependency property.</summary>
        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register(
                nameof(Visible),
                typeof(bool),
                typeof(ColumnDefinitionCollapsable),
                new PropertyMetadata(true, OnVisibleChanged));

        private static void OnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(WidthProperty);
            d.CoerceValue(MinWidthProperty);
        }

        /// <summary>Gets or sets a value indicating whether the column is visible; when <c>false</c> the column collapses to zero width.</summary>
        public bool Visible
        {
            get => (bool)GetValue(VisibleProperty);
            set => SetValue(VisibleProperty, value);
        }

        #endregion
    }
}
