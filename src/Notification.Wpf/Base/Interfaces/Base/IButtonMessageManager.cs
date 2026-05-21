using System;
using Notification.Wpf.Base.Interfaces.Options;
using Notification.Wpf.Classes;
using Notifications.Wpf.Annotations;

namespace Notification.Wpf.Base.Interfaces.Base
{
    public interface IButtonMessageManager
    {

        /// <summary>
        /// Show message with button to open file or directory (file destination)
        /// Показывает всплывающее окно для открытия файла или директории
        /// </summary>
        /// <param name="FilePath">File path</param>
        /// <param name="ShowFile">show file button</param>
        /// <param name="ShowDirectory">show directory button</param>
        /// <param name="options">customization options</param>
        /// <param name="image">image if need</param>
        /// <param name="ExpirationTime">time after which the window will disappear </param>
        /// <param name="AreaName">window area</param>
        /// <param name="ShowXbtn">Show X close button</param>
        public void ShowFilePopUpMessage(
            string FilePath, bool ShowFile = true, bool ShowDirectory = true,
            TimeSpan? ExpirationTime = null, string AreaName = "",
            ICustomizedOptions options = null,
            NotificationImage image = null,
            bool ShowXbtn = true);

        /// <summary>
        /// Show message with buttons
        /// </summary>
        /// <param name="Message">Message</param>
        /// <param name="Title">Title</param>
        /// <param name="LeftButtonAction">Left button action (if null - not visible)</param>
        /// <param name="LeftButtonContent">Left button content (possible text) - if null - "Cancel"</param>
        /// <param name="RightButtonAction">Right button action (if null - not visible)</param>
        /// <param name="RightButtonContent">Right button content (possible text) - if null - "Ok"</param>
        /// <param name="ExpirationTime">time after which the window will disappear</param>
        /// <param name="AreaName">area name</param>
        /// <param name="Image">image</param>
        /// <param name="options"> Customization options </param>
        /// <param name="ShowXbtn">Show X close button</param>
        /// <example>
        /// <code>
        /// Notifier.ShowInfoWindow("The picture is successfully added","Success",
        /// RightButton:(Sender, Args) =&gt; _ = Process.Start(report_file.FullName), RightButtonContent:"Open file",
        /// TimeSpan:TimeSpan.FromSeconds(10));
        /// </code>
        /// </example>
        void ShowButtonWindow(
            string Message, [CanBeNull] string Title = null,
            [CanBeNull] Action LeftButtonAction = null, string LeftButtonContent = null,
            [CanBeNull] Action RightButtonAction = null, string RightButtonContent = null,
            TimeSpan? ExpirationTime = null, string AreaName = "",
            ICustomizedOptions options = null,
            NotificationImage Image = null,
            bool ShowXbtn = true);

    }
}
