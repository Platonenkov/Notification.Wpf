using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Notification.Wpf.Classes
{
    /// <summary> Are absolute position </summary>
    public class AbsolutePosition
    {
        /// <summary> X position </summary>
        public double X { get; set; }
        /// <summary> Y position </summary>
        public double Y { get; set; }

        /// <summary> Base corner </summary>
        public Corner BaseCorner { get; set; }

        /// <summary> position </summary>
        public Thickness Margin => BaseCorner switch
        {
            Corner.TopLeft => new Thickness(X, Y, 0, 0),
            Corner.TopRight => new Thickness(0, Y, X, 0),
            Corner.BottomLeft => new Thickness(X, 0, 0, Y),
            Corner.BottomRight => new Thickness(0, 0, X, Y),
            _ => throw new ArgumentOutOfRangeException()
        };
        /// <summary> Horizontal Alignment </summary>
        public HorizontalAlignment HorAlign => BaseCorner switch
        {
            Corner.TopLeft => HorizontalAlignment.Left,
            Corner.TopRight => HorizontalAlignment.Right,
            Corner.BottomLeft => HorizontalAlignment.Left,
            Corner.BottomRight => HorizontalAlignment.Right,
            _ => throw new ArgumentOutOfRangeException()
        };
        /// <summary> Horizontal Alignment </summary>
        public VerticalAlignment VertAlign => BaseCorner switch
        {
            Corner.TopLeft => VerticalAlignment.Top,
            Corner.TopRight => VerticalAlignment.Top,
            Corner.BottomLeft => VerticalAlignment.Bottom,
            Corner.BottomRight => VerticalAlignment.Bottom,
            _ => throw new ArgumentOutOfRangeException()
        };

    }
}
