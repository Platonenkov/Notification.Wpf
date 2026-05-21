using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Notification.Wpf.Sample.Helpers
{
    /// <summary>
    /// Provides attached properties that size a <see cref="TextBlock"/> by a fixed or maximum number of text lines.
    /// </summary>
    public class NotificationLineHeightBehavior
    {
        #region Lines count

        /// <summary>
        /// Identifies the Lines attached property that sets the exact height of a <see cref="TextBlock"/> in lines.
        /// </summary>
        public static readonly DependencyProperty LinesProperty =
            DependencyProperty.RegisterAttached(
                "Lines",
                typeof(int),
                typeof(NotificationLineHeightBehavior),
                new PropertyMetadata(default(int), OnLinesPropertyChangedCallback));

        /// <summary>
        /// Sets the value of the Lines attached property for the specified <see cref="TextBlock"/>.
        /// </summary>
        /// <param name="element">The text block to set the value on.</param>
        /// <param name="value">The number of lines that defines the text block height.</param>
        public static void SetLines(TextBlock element, int value) => element.SetValue(LinesProperty, value);

        /// <summary>
        /// Gets the value of the Lines attached property for the specified <see cref="TextBlock"/>.
        /// </summary>
        /// <param name="element">The text block to read the value from.</param>
        /// <returns>The number of lines that defines the text block height.</returns>
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

        /// <summary>
        /// Identifies the MaxLines attached property that sets the maximum height of a <see cref="TextBlock"/> in lines.
        /// </summary>
        public static readonly DependencyProperty MaxLinesProperty =
            DependencyProperty.RegisterAttached(
                "MaxLines",
                typeof(int),
                typeof(NotificationLineHeightBehavior),
                new PropertyMetadata(default(int), OnMaxLinesPropertyChangedCallback));

        /// <summary>
        /// Sets the value of the MaxLines attached property for the specified <see cref="TextBlock"/>.
        /// </summary>
        /// <param name="element">The text block to set the value on.</param>
        /// <param name="value">The maximum number of lines that defines the text block height.</param>
        public static void SetMaxLines(TextBlock element, int value) => element.SetValue(MaxLinesProperty, value);

        /// <summary>
        /// Gets the value of the MaxLines attached property for the specified <see cref="TextBlock"/>.
        /// </summary>
        /// <param name="element">The text block to read the value from.</param>
        /// <returns>The maximum number of lines that defines the text block height.</returns>
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
