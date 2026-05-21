namespace Notification.Core
{
    /// <summary>
    /// Specifies the screen position where notifications are displayed.
    /// </summary>
    public enum NotificationPosition
    {
        /// <summary>
        /// Top-left corner of the screen.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Top-right corner of the screen.
        /// </summary>
        TopRight,

        /// <summary>
        /// Top-center of the screen.
        /// </summary>
        TopCenter,

        /// <summary>
        /// Bottom-left corner of the screen.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Bottom-right corner of the screen.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Bottom-center of the screen.
        /// </summary>
        BottomCenter,

        /// <summary>
        /// Center-left edge of the screen.
        /// </summary>
        CenterLeft,

        /// <summary>
        /// Center-right edge of the screen.
        /// </summary>
        CenterRight,

        /// <summary>
        /// Center of the screen.
        /// </summary>
        Center,

        /// <summary>
        /// An absolute position specified by explicit coordinates.
        /// </summary>
        Absolute
    }
}
