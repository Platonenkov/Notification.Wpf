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

        #region Default Icons

        //public static SolidColorBrush DefaultIconColorBrush = new(Colors.White);

        //public static object SuccessIcon = new Path()
        //{
        //    Data = Geometry.Parse("M 15.56055 5.9323048e-7 7.53125 10.197261 2.73242 5.2304706 0 7.8710906 7.82422 15.968751 18.54492 2.3515606 15.56055 5.9323048e-7 Z"),
        //    Fill = DefaultIconColorBrush
        //};
        //public static object ErrorIcon = new SvgAwesome() { Icon = EFontAwesomeIcon.Solid_Bug, Height = 20, Foreground = DefaultIconColorBrush };
        //public static object WarningIcon = new Path()
        //{
        //    Data = Geometry.Parse("M 12.414089 4.6396565e-7 C 12.128679 -9.5360343e-6 11.832699 0.06810046 11.574249 0.19726046 c -0.29252 0.14627 -0.55012 0.39584 -0.70899 0.67383 l -0.002 0.002 L 0.22067905 19.77348 l -0.0117 0.0234 C 0.08326905 20.04831 -9.5367432e-7 20.33976 -9.5367432e-7 20.64844 c 0 0.30629 0.0851000036743 0.62597 0.23633000367432 0.89063 0.13469 0.2357 0.31957 0.44504 0.5332 0.60937 l 0.0137 0.0117 0.0156 0.01 C 1.076789 22.36867 1.440719 22.48654 1.785149 22.48654 l 21.28516 0 c 0.3398 0 0.70907 -0.12364 0.98828 -0.33398 0.2208 -0.16158 0.42089 -0.3689 0.56055 -0.61328 0.15122 -0.26466 0.23633 -0.58434 0.23633 -0.89063 0 -0.30868 -0.0852 -0.60013 -0.21094 -0.85156 l -0.01 -0.0234 -10.66992 -18.90038954 -0.002 -0.002 c -0.15887 -0.27808 -0.41633 -0.52756 -0.70899 -0.67383 -0.25845 -0.12918 -0.55443 -0.19726999603 -0.83984 -0.19725999603 z m 0 2.19531003603435 10.32617 18.2910095 -20.625 0 10.29883 -18.2910095 z m -1.48633 3.84765 0 8.9121095 3 0 0 -8.9121095 -3 0 z m 0 10.3808595 0 3 3 0 0 -3 -3 0 z"),
        //    Fill = DefaultIconColorBrush
        //};
        //public static object InformationIcon = new Path()
        //{
        //    Data = Geometry.Parse("M 10.968748 8.9809305e-8 C 4.9320181 8.9809305e-8 -1.9073487e-6 4.9320201 -1.9073487e-6 10.96875 c 0 6.03672 4.9322600073487 10.9668 10.9687499073487 10.9668 6.03648 0 10.96875 -4.93008 10.96875 -10.9668 C 21.937498 4.9320201 17.005458 8.9809305e-8 10.968748 8.9809305e-8 Z m 0 2.000000010190695 c 4.95043 0 8.96875 4.0183 8.96875 8.9687499 0 4.95044 -4.01809 8.9668 -8.96875 8.9668 -4.9506899 0 -8.9687499 -4.01636 -8.9687499 -8.9668 0 -4.9504499 4.0183 -8.9687499 8.9687499 -8.9687499 z m -1.4999999 2.49805 0 3 2.9999999 0 0 -3 -2.9999999 0 z m 0 4.4707 0 8.9101599 2.9999999 0 0 -8.9101599 -2.9999999 0 z"),
        //    Fill = Brushes.White
        //};
        //public static object NotificationIcon = InformationIcon;

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
