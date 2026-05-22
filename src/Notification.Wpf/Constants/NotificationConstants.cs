using System.Windows;
using System.Windows.Media;
using Notification.Core;
using Notification.Wpf.Adapters;
using Notification.Wpf.Base;
using Notification.Wpf.Classes;
using Notification.Wpf.Controls;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Notification.Wpf.Constants
{
    /// <summary> Notification static settings </summary>
    public class NotificationConstants
    {
        internal static INotificationConfiguration _config;

        /// <summary> Bind configuration from DI </summary>
        public static void Bind(INotificationConfiguration config)
        {
            _config = config;

            NotificationsOverlayWindowMaxCount = config.MaxOverlayWindowCount;
            CollapseProgressIfMoreRows = config.CollapseProgressIfMoreRows;
            MessagePosition = config.MessagePosition;
            MinWidth = config.MinWidth;
            MaxWidth = config.MaxWidth;

            DefaultBackgroundColor = config.DefaultBackgroundColor.ToBrush();
            DefaultForegroundColor = config.DefaultForegroundColor.ToBrush();
            SuccessBackgroundColor = config.SuccessBackgroundColor.ToBrush();
            WarningBackgroundColor = config.WarningBackgroundColor.ToBrush();
            ErrorBackgroundColor = config.ErrorBackgroundColor.ToBrush();
            InformationBackgroundColor = config.InformationBackgroundColor.ToBrush();
            DefaultProgressColor = config.DefaultProgressColor.ToBrush();

            DefaultIconColor = config.DefaultIconColor.ToBrush();
            SuccessIconColor = config.SuccessIconColor.ToBrush();
            WarningIconColor = config.WarningIconColor.ToBrush();
            ErrorIconColor = config.ErrorIconColor.ToBrush();
            InformationIconColor = config.InformationIconColor.ToBrush();

            BaseTextSize = config.BaseTextSize;
            TitleSize = config.TitleSize;
            MessageSize = config.MessageSize;
            FontName = config.FontName;
            TitleTextAlignment = config.TitleTextAlignment.ToWpfTextAlignment();
            MessageTextAlignment = config.MessageTextAlignment.ToWpfTextAlignment();
            DefaultRowCounts = config.DefaultRowCount;
            DefaulTextTrimType = config.DefaultTrimType;

            CancellationMessage = config.CancellationMessage;
            OpenFileMessage = config.OpenFileMessage;
            OpenFolderMessage = config.OpenFolderMessage;
            DefaultLeftButtonContent = config.DefaultLeftButtonContent;
            DefaultRightButtonContent = config.DefaultRightButtonContent;
            DefaultProgressButtonContent = config.DefaultProgressButtonContent;
        }

        /// <summary> Overlay window maximum count </summary>
        public static uint NotificationsOverlayWindowMaxCount { get; set; } = 999;
        /// <summary> If messages count in overlay window will be more that maximum - progress bar will start collapsed (progress bar never closing automatically) </summary>
        public static bool CollapseProgressIfMoreRows { get; set; } = true;

        /// <summary> Overlay message position </summary>
        public static NotificationPosition MessagePosition { get; set; } = NotificationPosition.BottomRight;

        /// <summary> Whether the notification overlay window stays on top of other windows (issue #65) </summary>
        public static bool OverlayWindowTopmost { get; set; } = true;

        /// <summary> Pause the auto-close timer while the mouse is over the notification (issue #71) </summary>
        public static bool KeepNotificationVisibleOnMouseOver { get; set; }

        /// <summary> Corner radius of the notification card (issue #52) </summary>
        public static CornerRadius NotificationCornerRadius { get; set; } = new CornerRadius(0);

        /// <summary> Reverse are when absolute </summary>
        public static bool? IsReversedPanel { get; set; }

        /// <summary> Default message position when position absolute </summary>
        public static AbsolutePosition AbsolutePosition { get; } = new AbsolutePosition();

        #region Notification

        #region Default colors

        /// <summary> base background color </summary>
        public static Brush DefaultBackgroundColor { get; set; } = (Brush)new BrushConverter().ConvertFrom("#FF444444");
        /// <summary> base foreground color </summary>
        public static Brush DefaultForegroundColor { get; set; } = new SolidColorBrush(Colors.WhiteSmoke);

        /// <summary> base background color </summary>
        public static Brush SuccessBackgroundColor { get; set; } = new SolidColorBrush(Colors.LimeGreen);
        /// <summary> base background color </summary>
        public static Brush WarningBackgroundColor { get; set; } = new SolidColorBrush(Colors.Orange);
        /// <summary> base background color </summary>
        public static Brush ErrorBackgroundColor { get; set; } = new SolidColorBrush(Colors.OrangeRed);
        /// <summary> base background color </summary>
        public static Brush InformationBackgroundColor { get; set; } = new SolidColorBrush(Colors.CornflowerBlue);

        /// <summary> default progress line foreground </summary>
        public static Brush DefaultProgressColor { get; set; } = (Brush)new BrushConverter().ConvertFrom("#FF01D328");
        #endregion

        #region Icon colors

        /// <summary> built-in icon color for notifications without a specific type </summary>
        public static Brush DefaultIconColor { get; set; } = new SolidColorBrush(Colors.White);
        /// <summary> built-in icon color for success notifications </summary>
        public static Brush SuccessIconColor { get; set; } = new SolidColorBrush(Colors.White);
        /// <summary> built-in icon color for warning notifications </summary>
        public static Brush WarningIconColor { get; set; } = new SolidColorBrush(Colors.White);
        /// <summary> built-in icon color for error notifications </summary>
        public static Brush ErrorIconColor { get; set; } = new SolidColorBrush(Colors.White);
        /// <summary> built-in icon color for information notifications </summary>
        public static Brush InformationIconColor { get; set; } = new SolidColorBrush(Colors.White);

        #endregion


        #region Text trim and row count

        /// <summary> visible rows count in message by default</summary>
        public static uint DefaultRowCounts { get; set; } = 2U;
        /// <summary>default Notification text trim type </summary>
        public static NotificationTextTrimType DefaulTextTrimType { get; set; } = NotificationTextTrimType.NoTrim;

        #endregion

        #region Text properties

        #region Size

        /// <summary> Default text size </summary>
        public static double BaseTextSize { get; set; } = 14D;
        /// <summary> Default Title text size </summary>
        public static double TitleSize { get; set; } = BaseTextSize;
        /// <summary> Default Message text size </summary>
        public static double MessageSize { get; set; } = BaseTextSize;

        #endregion

        /// <summary> Default FontName </summary>
        public static string FontName { get; set; } = "Segoe UI";

        #region Text alignment

        /// <summary> Default Title text alignment </summary>
        public static TextAlignment TitleTextAlignment { get; set; } = TextAlignment.Left;
        /// <summary> Default Message text alignment </summary>
        public static TextAlignment MessageTextAlignment { get; set; } = TextAlignment.Left;

        #endregion

        /// <summary> Title text settings </summary>
        public static TextContentSettings TitleSettings => new ()
        {
            FontFamily = new FontFamily(FontName),
            FontSize = TitleSize,
            FontStyle = FontStyles.Normal,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalTextAlignment = VerticalAlignment.Stretch,
            Opacity = 1,
            TextAlignment = TitleTextAlignment
        };
        /// <summary> Message text settings </summary>
        public static TextContentSettings MessageSettings => new ()
        {
            FontFamily = new FontFamily(FontName),
            FontSize = MessageSize,
            FontStyle = FontStyles.Normal,
            FontWeight = FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalTextAlignment = VerticalAlignment.Stretch,
            Opacity = 0.8,
            TextAlignment = MessageTextAlignment
        };

        #endregion

        #endregion

        #region Message size
        /// <summary> Notification MinWidth (if MaxWidth less than MinWidth = MaxWidth) </summary>
        public static double MinWidth { get; set; } = 350D;
        /// <summary> Notification MaxWidth</summary>
        public static double MaxWidth { get; set; } = 350D;

        #endregion

        #region Default text


        /// <summary> Default message for Show Cancellation </summary>
        public static string CancellationMessage { get; set; } = "Operation was cancelled";
        /// <summary> Open file button text </summary>
        public static string OpenFileMessage { get; set; } = "Open File";
        /// <summary> Open folder button text </summary>
        public static string OpenFolderMessage { get; set; } = "Open Folder";

        /// <summary>default Notification left button content </summary>
        public static object DefaultLeftButtonContent { get; set; } = "Ok";
        /// <summary>default Notification right button content </summary>
        public static object DefaultRightButtonContent { get; set; } = "Cancel";
        /// <summary> Cancel button content </summary>
        public static object DefaultProgressButtonContent { get; set; } = "Cancel";

        #endregion

        /// <summary> work area height </summary>
        public static double AreaHeight => SystemParameters.WorkArea.Height;
        /// <summary> work area width </summary>
        public static double AreaWidth => SystemParameters.WorkArea.Width;

    }
}
