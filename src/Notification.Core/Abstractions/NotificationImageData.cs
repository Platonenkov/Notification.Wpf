namespace Notification.Core
{
    /// <summary>
    /// Represents image data and layout settings for a notification image.
    /// </summary>
    public class NotificationImageData
    {
        /// <summary>Gets or sets the raw image bytes. Used when the image is provided as in-memory data.</summary>
        public byte[] RawData { get; set; }

        /// <summary>Gets or sets the URI of the image resource. Used when the image is loaded from a file or URL.</summary>
        public string Uri { get; set; }

        /// <summary>Gets or sets the position of the image within the notification layout.</summary>
        public ImagePosition Position { get; set; } = ImagePosition.None;
    }
}
