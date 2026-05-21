using System.Windows;
using System.Windows.Controls;

namespace Notifications.Wpf.Extensions
{
    /// <summary>
    /// A <see cref="RowDefinition"/> that can be collapsed to zero height via the <see cref="Visible"/> property.
    /// </summary>
    public class RowDefinitionCollapsable : RowDefinition
    {
        static RowDefinitionCollapsable()
        {
            HeightProperty.OverrideMetadata(
                typeof(RowDefinitionCollapsable),
                new FrameworkPropertyMetadata(
                    new GridLength(1, GridUnitType.Star),
                    null,
                    (d, v) => ((RowDefinitionCollapsable)d).Visible ? v : new GridLength(0)));


            MinHeightProperty.OverrideMetadata(
                typeof(RowDefinitionCollapsable),
                new FrameworkPropertyMetadata(0d, null, (d, v) => ((RowDefinitionCollapsable)d).Visible ? v : 0d));
        }

        #region Visible : bool - Видимость

        /// <summary>Identifies the <see cref="Visible"/> dependency property.</summary>
        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register(
                nameof(Visible),
                typeof(bool),
                typeof(RowDefinitionCollapsable),
                new PropertyMetadata(true, OnVisibleChanged));

        private static void OnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(HeightProperty);
            d.CoerceValue(MinHeightProperty);
        }

        /// <summary>Gets or sets a value indicating whether the row is visible; when <c>false</c> the row collapses to zero height.</summary>
        public bool Visible
        {
            get => (bool)GetValue(VisibleProperty);
            set => SetValue(VisibleProperty, value);
        }

        #endregion
    }
}
