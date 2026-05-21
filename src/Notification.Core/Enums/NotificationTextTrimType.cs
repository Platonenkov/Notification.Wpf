namespace Notification.Core
{
    /// <summary>
    /// Specifies how notification text is trimmed when it exceeds the available space.
    /// </summary>
    public enum NotificationTextTrimType
    {
        /// <summary>
        /// No trimming is applied; text is displayed in full.
        /// </summary>
        NoTrim,

        /// <summary>
        /// Text is trimmed with an ellipsis when it exceeds the available space.
        /// </summary>
        Trim,

        /// <summary>
        /// A "more" link or indicator is attached to the trimmed text.
        /// </summary>
        Attach,

        /// <summary>
        /// A "more" link or indicator is attached only when the text exceeds the maximum number of rows.
        /// </summary>
        AttachIfMoreRows
    }
}
