using System.Windows;
using Notification.Core;
using Notification.Wpf.Adapters;
using Xunit;

namespace Notification.Wpf.Tests
{
    public class TextSettingsExtensionsTests
    {
        [Theory]
        [InlineData(NotificationFontStyle.Normal)]
        [InlineData(NotificationFontStyle.Italic)]
        [InlineData(NotificationFontStyle.Oblique)]
        public void FontStyle_Roundtrips(NotificationFontStyle style)
        {
            FontStyle wpfStyle = style.ToWpfFontStyle();
            NotificationFontStyle coreStyle = wpfStyle.ToCoreStyle();

            Assert.Equal(style, coreStyle);
        }

        [Theory]
        [InlineData(NotificationFontWeight.Thin)]
        [InlineData(NotificationFontWeight.Normal)]
        [InlineData(NotificationFontWeight.Bold)]
        [InlineData(NotificationFontWeight.Black)]
        public void FontWeight_Roundtrips(NotificationFontWeight weight)
        {
            FontWeight wpfWeight = weight.ToWpfFontWeight();
            NotificationFontWeight coreWeight = wpfWeight.ToCoreWeight();

            Assert.Equal(weight, coreWeight);
        }

        [Theory]
        [InlineData(NotificationTextAlignment.Left)]
        [InlineData(NotificationTextAlignment.Center)]
        [InlineData(NotificationTextAlignment.Right)]
        [InlineData(NotificationTextAlignment.Justify)]
        public void TextAlignment_Roundtrips(NotificationTextAlignment alignment)
        {
            TextAlignment wpfAlignment = alignment.ToWpfTextAlignment();
            NotificationTextAlignment coreAlignment = wpfAlignment.ToCoreAlignment();

            Assert.Equal(alignment, coreAlignment);
        }

        [Theory]
        [InlineData(NotificationHorizontalAlignment.Left)]
        [InlineData(NotificationHorizontalAlignment.Center)]
        [InlineData(NotificationHorizontalAlignment.Right)]
        [InlineData(NotificationHorizontalAlignment.Stretch)]
        public void HorizontalAlignment_Roundtrips(NotificationHorizontalAlignment alignment)
        {
            HorizontalAlignment wpfAlignment = alignment.ToWpfHorizontalAlignment();
            NotificationHorizontalAlignment coreAlignment = wpfAlignment.ToCoreHorizontalAlignment();

            Assert.Equal(alignment, coreAlignment);
        }

        [Theory]
        [InlineData(NotificationVerticalAlignment.Top)]
        [InlineData(NotificationVerticalAlignment.Center)]
        [InlineData(NotificationVerticalAlignment.Bottom)]
        [InlineData(NotificationVerticalAlignment.Stretch)]
        public void VerticalAlignment_Roundtrips(NotificationVerticalAlignment alignment)
        {
            VerticalAlignment wpfAlignment = alignment.ToWpfVerticalAlignment();
            NotificationVerticalAlignment coreAlignment = wpfAlignment.ToCoreVerticalAlignment();

            Assert.Equal(alignment, coreAlignment);
        }
    }
}
