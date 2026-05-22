using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Notification.Wpf.Base.Interfaces.Options
{
    /// <summary> Customized options for notification </summary>
    public interface ICustomizedOptions
    {
        /// <summary> icon in left bar side </summary>
        public object Icon { get; set; }
        /// <summary> Color applied to the built-in type icon. When null, the per-type color from configuration is used. Ignored when a custom <see cref="Icon"/> is supplied. </summary>
        public Brush IconForeground { get; set; }
        /// <summary> Notification background </summary>
        public Brush Background { get; set; }
        /// <summary> Text foreground </summary>
        public Brush Foreground { get; set; }
        /// <summary> Trimming long text if need </summary>
        public NotificationTextTrimType TrimType { get; set; }

        /// <summary> Set rows of message that will show if set Trim </summary>
        public uint RowsCount { get; set; }

        #region Text settings
        /// <summary> Title text style settings </summary>
        public TextContentSettings TitleTextSettings { get; set; }
        /// <summary> Message text style settings </summary>
        public TextContentSettings MessageTextSettings { get; set; }
        #endregion

    }
}
