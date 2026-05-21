namespace Notification.Core
{
    public readonly struct NotificationProgressReport
    {
        public double? Value { get; }
        public string Message { get; }
        public string Title { get; }
        public bool? ShowCancel { get; }

        public NotificationProgressReport(double? value, string message, string title, bool? showCancel)
        {
            Value = value;
            Message = message;
            Title = title;
            ShowCancel = showCancel;
        }
    }
}
