using System.Collections.Generic;
using Notification.Core;
using Notification.Core.Internal;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationButtonResolverTests
    {
        [Fact]
        public void Returns_explicit_buttons_when_provided()
        {
            NotificationRequest req = new()
            {
                Buttons = new[]
                {
                    new NotificationButton { Id = "yes", Content = "Yes", Role = NotificationButtonRole.Affirmative },
                    new NotificationButton { Id = "no",  Content = "No",  Role = NotificationButtonRole.Negative }
                }
            };

            IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

            Assert.Equal(2, result.Count);
            Assert.Equal("yes", result[0].Id);
        }

        [Fact]
        public void Converts_legacy_left_content_to_button_with_synthetic_id()
        {
            NotificationRequest req = new();
#pragma warning disable CS0618
            req.LeftButtonContent = "OK";
#pragma warning restore CS0618

            IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

            Assert.Single(result);
            Assert.Equal("left", result[0].Id);
            Assert.Equal("OK", result[0].Content);
            Assert.Equal(NotificationButtonRole.Affirmative, result[0].Role);
        }

        [Fact]
        public void Converts_legacy_right_content_to_button_with_synthetic_id()
        {
            NotificationRequest req = new();
#pragma warning disable CS0618
            req.RightButtonContent = "Cancel";
#pragma warning restore CS0618

            IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

            Assert.Single(result);
            Assert.Equal("right", result[0].Id);
            Assert.Equal(NotificationButtonRole.Negative, result[0].Role);
        }

        [Fact]
        public void Converts_both_legacy_fields_in_left_then_right_order()
        {
            NotificationRequest req = new();
#pragma warning disable CS0618
            req.LeftButtonContent = "OK";
            req.RightButtonContent = "Cancel";
#pragma warning restore CS0618

            IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

            Assert.Equal(2, result.Count);
            Assert.Equal("left", result[0].Id);
            Assert.Equal("right", result[1].Id);
        }

        [Fact]
        public void Returns_empty_when_neither_buttons_nor_legacy_populated()
        {
            NotificationRequest req = new();

            IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

            Assert.Empty(result);
        }

        [Fact]
        public void Buttons_collection_takes_priority_over_legacy_fields()
        {
            NotificationRequest req = new()
            {
                Buttons = new[] { new NotificationButton { Id = "primary", Content = "Primary" } }
            };
#pragma warning disable CS0618
            req.LeftButtonContent = "Ignored";
#pragma warning restore CS0618

            IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

            Assert.Single(result);
            Assert.Equal("primary", result[0].Id);
        }
    }
}
