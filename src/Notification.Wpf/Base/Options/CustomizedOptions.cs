using Notification.Wpf.Base.Interfaces.Options;
using Notification.Wpf.Constants;

using System.Windows;
using System.Windows.Media;

namespace Notification.Wpf.Base.Options
{
    /// <inheritdoc />
    public class CustomizedOptions : ICustomizedOptions
    {
        /// <inheritdoc />
        public object Icon { get; set; }

        /// <inheritdoc />
        public Brush Background { get; set; }

        /// <inheritdoc />
        public Brush Foreground { get; set; }
        /// <inheritdoc />
        public NotificationTextTrimType TrimType { get; set; } = NotificationConstants.DefaulTextTrimType;
        /// <inheritdoc />
        public uint RowsCount { get; set; } = NotificationConstants.DefaultRowCounts;

        /// <inheritdoc />
        public TextContentSettings TitleTextSettings { get; set; } = new()
        {
            FontStyle = FontStyles.Normal,
            FontFamily = new FontFamily(NotificationConstants.FontName),
            FontSize = NotificationConstants.TitleSize,
            FontWeight = FontWeights.Bold,
            TextAlignment = NotificationConstants.TitleTextAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalTextAlignment = VerticalAlignment.Stretch,
            Opacity = 1
        };
        /// <inheritdoc />
        public TextContentSettings MessageTextSettings { get; set; } = new()
        {
            FontStyle = FontStyles.Normal,
            FontFamily = new FontFamily(NotificationConstants.FontName),
            FontSize = NotificationConstants.MessageSize,
            FontWeight = FontWeights.Normal,
            TextAlignment = NotificationConstants.MessageTextAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalTextAlignment = VerticalAlignment.Stretch,
            Opacity = 0.8
        };


        /// <summary> Get valid options after check for null </summary>
        /// <param name="options">options</param>
        /// <returns></returns>
        public static ICustomizedOptions GetValidCustomizedOptions(ICustomizedOptions options) => new CustomizedOptions()
        {
            Background = options?.Background ?? NotificationConstants.DefaultBackgroundColor,
            Foreground = options?.Foreground ?? NotificationConstants.DefaultForegroundColor,
            Icon = options?.Icon,
            MessageTextSettings = options?.MessageTextSettings,
            TitleTextSettings = options?.TitleTextSettings,
            RowsCount = options?.RowsCount is { } count and > 0 ? count : 1,
            TrimType = options?.TrimType ?? NotificationConstants.DefaulTextTrimType
        };

    }
}
