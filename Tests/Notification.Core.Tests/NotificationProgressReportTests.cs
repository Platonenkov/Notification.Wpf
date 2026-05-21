using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationProgressReportTests
    {
        [Fact]
        public void Constructor_SetsAllProperties()
        {
            NotificationProgressReport report = new NotificationProgressReport(0.75, "Processing...", "Title", true);

            Assert.Equal(0.75, report.Value);
            Assert.Equal("Processing...", report.Message);
            Assert.Equal("Title", report.Title);
            Assert.Equal(true, report.ShowCancel);
        }

        [Fact]
        public void Constructor_AllNulls_Allowed()
        {
            NotificationProgressReport report = new NotificationProgressReport(null, null, null, null);

            Assert.Null(report.Value);
            Assert.Null(report.Message);
            Assert.Null(report.Title);
            Assert.Null(report.ShowCancel);
        }

        [Fact]
        public void IsReadonly_CannotBeModified()
        {
            NotificationProgressReport report = new NotificationProgressReport(0.5, "msg", "ttl", false);

            Assert.Equal(0.5, report.Value);
            Assert.Equal("msg", report.Message);
            Assert.Equal("ttl", report.Title);
            Assert.Equal(false, report.ShowCancel);
        }
    }
}
