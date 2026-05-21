using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Notification.Wpf.Sample.Helpers
{
    public class NotificationLineHeightBehavior
    {
        #region Lines count

        public static readonly DependencyProperty LinesProperty =
            DependencyProperty.RegisterAttached(
                "Lines",
                typeof(int),
                typeof(NotificationLineHeightBehavior),
                new PropertyMetadata(default(int), OnLinesPropertyChangedCallback));

        public static void SetLines(TextBlock element, int value) => element.SetValue(LinesProperty, value);

        public static int GetLines(TextBlock element) => (int)element.GetValue(LinesProperty);

        private static void OnLinesPropertyChangedCallback(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                if (textBlock.IsLoaded)
                {
                    SetLineHeight();
                }
                else
                {
                    textBlock.Loaded += OnLoaded;

                    void OnLoaded(object _, RoutedEventArgs __)
                    {
                        textBlock.Loaded -= OnLoaded;
                        SetLineHeight();
                    }
                }

                void SetLineHeight()
                {
                    double lineHeight =
                        double.IsNaN(textBlock.LineHeight)
                            ? textBlock.FontFamily.LineSpacing * textBlock.FontSize
                            : textBlock.LineHeight;
                    textBlock.Height = Math.Ceiling(lineHeight * GetLines(textBlock));
                }
            }
        }

        #endregion

        #region MaxLines

        public static readonly DependencyProperty MaxLinesProperty =
            DependencyProperty.RegisterAttached(
                "MaxLines",
                typeof(int),
                typeof(NotificationLineHeightBehavior),
                new PropertyMetadata(default(int), OnMaxLinesPropertyChangedCallback));

        public static void SetMaxLines(TextBlock element, int value) => element.SetValue(MaxLinesProperty, value);

        public static int GetMaxLines(TextBlock element) => (int)element.GetValue(MaxLinesProperty);

        private static void OnMaxLinesPropertyChangedCallback(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                if (textBlock.IsLoaded)
                {
                    SetLineHeight();
                }
                else
                {
                    textBlock.Loaded += OnLoaded;

                    void OnLoaded(object _, RoutedEventArgs __)
                    {
                        textBlock.Loaded -= OnLoaded;
                        SetLineHeight();
                    }
                }

                void SetLineHeight()
                {
                    double lineHeight =
                        double.IsNaN(textBlock.LineHeight)
                            ? textBlock.FontFamily.LineSpacing * textBlock.FontSize
                            : textBlock.LineHeight;
                    textBlock.MaxHeight = Math.Ceiling(lineHeight * GetMaxLines(textBlock));
                }
            }
        }

        #endregion
    }
}
