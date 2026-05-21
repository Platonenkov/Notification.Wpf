using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Notification.Wpf.Controls
{
    /// <summary>
    /// A <see cref="StackPanel"/> that can arrange its children in reverse order.
    /// </summary>
    public class ReversibleStackPanel : StackPanel
    {


        /// <summary>Gets or sets a value indicating whether children are arranged in reverse order.</summary>
        public bool ReverseOrder
        {
            get => (bool)GetValue(ReverseOrderProperty);
            set => SetValue(ReverseOrderProperty, value);
        }

        /// <summary>Identifies the <see cref="ReverseOrder"/> dependency property.</summary>
        public static readonly DependencyProperty ReverseOrderProperty =
            DependencyProperty.Register("ReverseOrder", typeof(bool), typeof(ReversibleStackPanel), new PropertyMetadata(false));


        /// <summary>
        /// Arranges child elements, optionally in reverse order depending on <see cref="ReverseOrder"/>.
        /// </summary>
        /// <param name="arrangeSize">The size available to arrange the children.</param>
        /// <returns>The actual size used by the panel.</returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double x = 0;
            double y = 0;

            var children = ReverseOrder ? InternalChildren.Cast<UIElement>().Reverse() : InternalChildren.Cast<UIElement>();
            foreach (var child in children)
            {
                Size size;

                if (Orientation == Orientation.Horizontal)
                {
                    size = new Size(child.DesiredSize.Width, Math.Max(arrangeSize.Height, child.DesiredSize.Height));
                    child.Arrange(new Rect(new Point(x, y), size));
                    x += size.Width;
                }
                else
                {
                    size = new Size(Math.Max(arrangeSize.Width, child.DesiredSize.Width), child.DesiredSize.Height);
                    child.Arrange(new Rect(new Point(x, y), size));
                    y += size.Height;
                }
            }

            return Orientation == Orientation.Horizontal ? new Size(x, arrangeSize.Height) : new Size(arrangeSize.Width, y);
        }
    }
}
