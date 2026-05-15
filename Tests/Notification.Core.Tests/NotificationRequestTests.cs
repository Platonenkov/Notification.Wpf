using System;
using System.Collections.Generic;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationRequestTests
    {
        [Fact]
        public void NewRequest_HasGeneratedId()
        {
            NotificationRequest request = new NotificationRequest();

            Assert.NotEqual(Guid.Empty, request.Id);
        }

        [Fact]
        public void NewRequest_HasDefaultValues()
        {
            NotificationRequest request = new NotificationRequest();

            Assert.Equal(NotificationType.None, request.Type);
            Assert.Equal(NotificationPriority.Normal, request.Priority);
            Assert.True(request.CloseOnClick);
            Assert.True(request.ShowCloseButton);
            Assert.Equal(2u, request.RowsCount);
            Assert.Equal(NotificationTextTrimType.NoTrim, request.TrimType);
            Assert.Equal("", request.AreaName);
        }

        [Fact]
        public void Request_PropertiesAreSettable()
        {
            NotificationRequest request = new NotificationRequest
            {
                Title = "Test",
                Message = "Body",
                Type = NotificationType.Error,
                Priority = NotificationPriority.Critical,
                ExpirationTime = TimeSpan.FromSeconds(30),
                CloseOnClick = false,
                ShowCloseButton = false,
                GroupKey = "grp",
                AreaName = "area1"
            };

            Assert.Equal("Test", request.Title);
            Assert.Equal("Body", request.Message);
            Assert.Equal(NotificationType.Error, request.Type);
            Assert.Equal(NotificationPriority.Critical, request.Priority);
            Assert.Equal(TimeSpan.FromSeconds(30), request.ExpirationTime);
            Assert.False(request.CloseOnClick);
            Assert.False(request.ShowCloseButton);
            Assert.Equal("grp", request.GroupKey);
            Assert.Equal("area1", request.AreaName);
        }

        [Fact]
        public void Extensions_CanStoreArbitraryData()
        {
            NotificationRequest request = new NotificationRequest
            {
                Extensions = new Dictionary<string, object>
                {
                    ["key1"] = "value",
                    ["key2"] = 123
                }
            };

            Assert.Equal("value", request.Extensions["key1"]);
            Assert.Equal(123, request.Extensions["key2"]);
        }

        [Fact]
        public void Callbacks_AreInvocable()
        {
            bool clickFired = false;
            bool closeFired = false;

            NotificationRequest request = new NotificationRequest
            {
                OnClick = () => clickFired = true,
                OnClose = () => closeFired = true
            };

            request.OnClick();
            request.OnClose();

            Assert.True(clickFired);
            Assert.True(closeFired);
        }
    }
}
