using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using MathCore.WPF.Converters;
using MathCore.WPF.Converters.Base;
using Notification.Wpf.Classes;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;

namespace Notification.Wpf.Sample.Helpers
{
    [ValueConversion(typeof(NotificationPosition), typeof(VerticalAlignment)), MarkupExtensionReturnType(typeof(VerticalPositionConverter))]
    internal class VerticalPositionConverter : ValueConverter
    {
        protected override object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is not NotificationPosition pos)
                return Binding.DoNothing;
            return pos switch
            {
                NotificationPosition.BottomRight => VerticalAlignment.Bottom,
                NotificationPosition.TopRight => VerticalAlignment.Top,
                NotificationPosition.BottomLeft => VerticalAlignment.Bottom,
                NotificationPosition.TopLeft => VerticalAlignment.Top,
                NotificationPosition.BottomCenter => VerticalAlignment.Bottom,
                NotificationPosition.TopCenter => VerticalAlignment.Top,
                NotificationPosition.Center => VerticalAlignment.Center,
                NotificationPosition.CenterLeft => VerticalAlignment.Center,
                NotificationPosition.CenterRight => VerticalAlignment.Center,
                NotificationPosition.Absolute=>NotificationConstants.AbsolutePosition.VertAlign,
                _ => throw new ArgumentOutOfRangeException()
            };
        }


        protected override object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();

    }
    [ValueConversion(typeof(NotificationPosition), typeof(HorizontalAlignment)), MarkupExtensionReturnType(typeof(HorizontalPositionConverter))]
    internal class HorizontalPositionConverter : ValueConverter
    {
        protected override object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is not NotificationPosition pos)
                return Binding.DoNothing;
            return pos switch
            {
                NotificationPosition.BottomRight => HorizontalAlignment.Right,
                NotificationPosition.TopRight => HorizontalAlignment.Right,
                NotificationPosition.BottomLeft => HorizontalAlignment.Left,
                NotificationPosition.TopLeft => HorizontalAlignment.Left,
                NotificationPosition.BottomCenter => HorizontalAlignment.Center,
                NotificationPosition.TopCenter => HorizontalAlignment.Center,
                NotificationPosition.Center => HorizontalAlignment.Center,
                NotificationPosition.CenterLeft => HorizontalAlignment.Left,
                NotificationPosition.CenterRight => HorizontalAlignment.Right,
                NotificationPosition.Absolute => NotificationConstants.AbsolutePosition.HorAlign,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        protected override object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();

    } 
    [ValueConversion(typeof(NotificationPosition), typeof(bool)), MarkupExtensionReturnType(typeof(AbsolutePositionConverter))]
    internal class AbsolutePositionConverter : ValueConverter
    {
        protected override object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is not NotificationPosition pos)
                return Binding.DoNothing;
            return pos == NotificationPosition.Absolute;
        }

        protected override object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();

    }
    [ValueConversion(typeof(AbsolutePosition), typeof(Thickness)), MarkupExtensionReturnType(typeof(CornerPositionMarginConverter))]
    internal class CornerPositionMarginConverter : ValueConverter
    {
        protected override object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is not AbsolutePosition pos)
                return Binding.DoNothing;
            return pos.Margin;
        }

        protected override object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();

    }
    [ValueConversion(typeof(AbsolutePosition), typeof(HorizontalAlignment)), MarkupExtensionReturnType(typeof(CornerPositionHorAlignConverter))]
    internal class CornerPositionHorAlignConverter : ValueConverter
    {
        protected override object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is not AbsolutePosition pos)
                return Binding.DoNothing;
            return pos.HorAlign;
        }

        protected override object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();

    }
    [ValueConversion(typeof(AbsolutePosition), typeof(VerticalAlignment)), MarkupExtensionReturnType(typeof(CornerPositionVertAlignConverter))]
    internal class CornerPositionVertAlignConverter : ValueConverter
    {
        protected override object Convert(object v, Type t, object p, CultureInfo c)
        {
            if (v is not AbsolutePosition pos)
                return Binding.DoNothing;
            return pos.VertAlign;
        }

        protected override object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();

    }
}
