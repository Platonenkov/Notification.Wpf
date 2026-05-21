namespace Notification.Core
{
    public class NotificationImageData
    {
        public byte[] RawData { get; set; }
        public string Uri { get; set; }
        public ImagePosition Position { get; set; } = ImagePosition.None;
    }
}
