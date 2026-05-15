using System;

namespace Notification.Wpf.Base.Interfaces.Options
{
    public interface IButtonOptions
    {
        #region Left button

        /// <summary>
        /// left button content
        /// </summary>
        public object LeftButtonContent { get; set; }

        /// <summary>
        /// Left button action
        /// </summary>
        public Action LeftButtonAction { get; set; }

        #endregion

        #region RightButton

        /// <summary>
        /// Right button content
        /// </summary>
        public object RightButtonContent { get; set; }

        /// <summary>
        /// Right button action
        /// </summary>
        public Action RightButtonAction { get; set; }

        #endregion

    }
}
