using System;
using Notification.Core;
using Notification.Core.Internal;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationRequestValidatorTests
    {
        [Fact]
        public void Null_request_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => NotificationRequestValidator.ValidateAsyncRequest(null));
        }

        [Fact]
        public void Valid_request_with_buttons_does_not_throw()
        {
            NotificationRequest req = new()
            {
                Buttons = new[]
                {
                    new NotificationButton { Id = "ok", Content = "OK", Role = NotificationButtonRole.Affirmative },
                    new NotificationButton { Id = "no", Content = "No", Role = NotificationButtonRole.Negative }
                }
            };

            NotificationRequestValidator.ValidateAsyncRequest(req);
        }

        [Fact]
        public void Request_without_buttons_does_not_throw()
        {
            NotificationRequestValidator.ValidateAsyncRequest(new NotificationRequest { Message = "x" });
        }

        [Fact]
        public void Legacy_only_request_does_not_throw()
        {
            NotificationRequest req = new();
#pragma warning disable CS0618
            req.LeftButtonContent = "OK";
#pragma warning restore CS0618
            NotificationRequestValidator.ValidateAsyncRequest(req);
        }

        [Fact]
        public void Buttons_plus_legacy_throws()
        {
            NotificationRequest req = new()
            {
                Buttons = new[] { new NotificationButton { Id = "ok", Content = "OK" } }
            };
#pragma warning disable CS0618
            req.LeftButtonContent = "Legacy";
#pragma warning restore CS0618

            Assert.Throws<ArgumentException>(() => NotificationRequestValidator.ValidateAsyncRequest(req));
        }

        [Fact]
        public void Duplicate_button_id_throws()
        {
            NotificationRequest req = new()
            {
                Buttons = new[]
                {
                    new NotificationButton { Id = "same", Content = "A" },
                    new NotificationButton { Id = "same", Content = "B" }
                }
            };

            Assert.Throws<ArgumentException>(() => NotificationRequestValidator.ValidateAsyncRequest(req));
        }

        [Fact]
        public void Null_button_throws()
        {
            NotificationRequest req = new()
            {
                Buttons = new NotificationButton[] { null }
            };

            Assert.Throws<ArgumentException>(() => NotificationRequestValidator.ValidateAsyncRequest(req));
        }

        [Fact]
        public void Empty_button_id_throws()
        {
            NotificationRequest req = new()
            {
                Buttons = new[] { new NotificationButton { Id = "", Content = "A" } }
            };

            Assert.Throws<ArgumentException>(() => NotificationRequestValidator.ValidateAsyncRequest(req));
        }

        [Fact]
        public void Null_button_content_throws()
        {
            NotificationRequest req = new()
            {
                Buttons = new[] { new NotificationButton { Id = "a", Content = null } }
            };

            Assert.Throws<ArgumentException>(() => NotificationRequestValidator.ValidateAsyncRequest(req));
        }
    }
}
