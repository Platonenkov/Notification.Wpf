using System;
using Xunit;

namespace Notification.Core.Tests
{
    public class NotificationBuilderTests
    {
        [Fact]
        public void Create_WithTitleAndMessage_SetsProperties()
        {
            NotificationRequest request = NotificationBuilder
                .Create("Test Title", "Test Message")
                .Build();

            Assert.Equal("Test Title", request.Title);
            Assert.Equal("Test Message", request.Message);
        }

        [Fact]
        public void Create_Empty_HasDefaultValues()
        {
            NotificationRequest request = NotificationBuilder.Create().Build();

            Assert.Null(request.Title);
            Assert.Null(request.Message);
            Assert.Equal(NotificationType.None, request.Type);
            Assert.Equal(NotificationPriority.Normal, request.Priority);
            Assert.NotEqual(Guid.Empty, request.Id);
        }

        [Fact]
        public void WithTitle_SetsTitle()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .WithTitle("Hello")
                .Build();

            Assert.Equal("Hello", request.Title);
        }

        [Fact]
        public void WithMessage_SetsMessage()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .WithMessage("World")
                .Build();

            Assert.Equal("World", request.Message);
        }

        [Theory]
        [InlineData(NotificationType.Success)]
        [InlineData(NotificationType.Warning)]
        [InlineData(NotificationType.Error)]
        [InlineData(NotificationType.Information)]
        public void OfType_SetsType(NotificationType type)
        {
            NotificationRequest request = NotificationBuilder.Create()
                .OfType(type)
                .Build();

            Assert.Equal(type, request.Type);
        }

        [Fact]
        public void AsSuccess_SetsTypeToSuccess()
        {
            NotificationRequest request = NotificationBuilder.Create().AsSuccess().Build();
            Assert.Equal(NotificationType.Success, request.Type);
        }

        [Fact]
        public void AsWarning_SetsTypeToWarning()
        {
            NotificationRequest request = NotificationBuilder.Create().AsWarning().Build();
            Assert.Equal(NotificationType.Warning, request.Type);
        }

        [Fact]
        public void AsError_SetsTypeToError()
        {
            NotificationRequest request = NotificationBuilder.Create().AsError().Build();
            Assert.Equal(NotificationType.Error, request.Type);
        }

        [Fact]
        public void AsInformation_SetsTypeToInformation()
        {
            NotificationRequest request = NotificationBuilder.Create().AsInformation().Build();
            Assert.Equal(NotificationType.Information, request.Type);
        }

        [Fact]
        public void InArea_SetsAreaName()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .InArea("MyArea")
                .Build();

            Assert.Equal("MyArea", request.AreaName);
        }

        [Fact]
        public void ExpiresIn_SetsExpirationTime()
        {
            TimeSpan expiration = TimeSpan.FromSeconds(10);
            NotificationRequest request = NotificationBuilder.Create()
                .ExpiresIn(expiration)
                .Build();

            Assert.Equal(expiration, request.ExpirationTime);
        }

        [Fact]
        public void ExpiresInSeconds_SetsExpirationTime()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .ExpiresInSeconds(7.5)
                .Build();

            Assert.Equal(TimeSpan.FromSeconds(7.5), request.ExpirationTime);
        }

        [Fact]
        public void NeverExpires_SetsMaxValue()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .NeverExpires()
                .Build();

            Assert.Equal(TimeSpan.MaxValue, request.ExpirationTime);
        }

        [Fact]
        public void CloseOnClick_DefaultTrue()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .CloseOnClick()
                .Build();

            Assert.True(request.CloseOnClick);
        }

        [Fact]
        public void CloseOnClick_False()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .CloseOnClick(false)
                .Build();

            Assert.False(request.CloseOnClick);
        }

        [Fact]
        public void HideCloseButton_SetsShowCloseButtonFalse()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .HideCloseButton()
                .Build();

            Assert.False(request.ShowCloseButton);
        }

        [Fact]
        public void WithPriority_SetsPriority()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .WithPriority(NotificationPriority.Critical)
                .Build();

            Assert.Equal(NotificationPriority.Critical, request.Priority);
        }

        [Fact]
        public void GroupAs_SetsGroupKey()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .GroupAs("group-1")
                .Build();

            Assert.Equal("group-1", request.GroupKey);
        }

        [Fact]
        public void WithBackground_Color_SetsBackgroundColor()
        {
            NotificationColor color = NotificationColor.FromRgb(255, 0, 0);
            NotificationRequest request = NotificationBuilder.Create()
                .WithBackground(color)
                .Build();

            Assert.Equal(color, request.BackgroundColor);
        }

        [Fact]
        public void WithBackground_Hex_SetsBackgroundColor()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .WithBackground("#FF0000")
                .Build();

            Assert.NotNull(request.BackgroundColor);
            Assert.Equal(255, request.BackgroundColor!.Value.R);
            Assert.Equal(0, request.BackgroundColor!.Value.G);
        }

        [Fact]
        public void WithForeground_Color_SetsForegroundColor()
        {
            NotificationColor color = NotificationColor.White;
            NotificationRequest request = NotificationBuilder.Create()
                .WithForeground(color)
                .Build();

            Assert.Equal(color, request.ForegroundColor);
        }

        [Fact]
        public void WithTitleSettings_ConfiguresSettings()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .WithTitleSettings(s =>
                {
                    s.FontSize = 24;
                    s.FontWeight = NotificationFontWeight.Bold;
                })
                .Build();

            Assert.NotNull(request.TitleSettings);
            Assert.Equal(24, request.TitleSettings!.FontSize);
            Assert.Equal(NotificationFontWeight.Bold, request.TitleSettings.FontWeight);
        }

        [Fact]
        public void WithMessageSettings_ConfiguresSettings()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .WithMessageSettings(s =>
                {
                    s.FontSize = 12;
                    s.FontStyle = NotificationFontStyle.Italic;
                })
                .Build();

            Assert.NotNull(request.MessageSettings);
            Assert.Equal(12, request.MessageSettings!.FontSize);
            Assert.Equal(NotificationFontStyle.Italic, request.MessageSettings.FontStyle);
        }

        [Fact]
        public void WithTrimming_SetsTrimTypeAndRows()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .WithTrimming(NotificationTextTrimType.Attach, 5)
                .Build();

            Assert.Equal(NotificationTextTrimType.Attach, request.TrimType);
            Assert.Equal(5u, request.RowsCount);
        }

        [Fact]
        public void WithLeftButton_SetsButtonProperties()
        {
            bool clicked = false;
            NotificationRequest request = NotificationBuilder.Create()
                .WithLeftButton("OK", () => clicked = true)
                .Build();

            Assert.Equal("OK", request.LeftButtonContent);
            Assert.NotNull(request.LeftButtonAction);
            request.LeftButtonAction!();
            Assert.True(clicked);
        }

        [Fact]
        public void WithRightButton_SetsButtonProperties()
        {
            bool clicked = false;
            NotificationRequest request = NotificationBuilder.Create()
                .WithRightButton("Cancel", () => clicked = true)
                .Build();

            Assert.Equal("Cancel", request.RightButtonContent);
            Assert.NotNull(request.RightButtonAction);
            request.RightButtonAction!();
            Assert.True(clicked);
        }

        [Fact]
        public void WithOkCancel_SetsBothButtons()
        {
            bool okClicked = false;
            bool cancelClicked = false;

            NotificationRequest request = NotificationBuilder.Create()
                .WithOkCancel(() => okClicked = true, () => cancelClicked = true)
                .Build();

            Assert.NotNull(request.LeftButtonAction);
            Assert.NotNull(request.RightButtonAction);

            request.LeftButtonAction!();
            request.RightButtonAction!();

            Assert.True(okClicked);
            Assert.True(cancelClicked);
        }

        [Fact]
        public void OnClick_SetsCallback()
        {
            bool clicked = false;
            NotificationRequest request = NotificationBuilder.Create()
                .OnClick(() => clicked = true)
                .Build();

            Assert.NotNull(request.OnClick);
            request.OnClick!();
            Assert.True(clicked);
        }

        [Fact]
        public void OnClose_SetsCallback()
        {
            bool closed = false;
            NotificationRequest request = NotificationBuilder.Create()
                .OnClose(() => closed = true)
                .Build();

            Assert.NotNull(request.OnClose);
            request.OnClose!();
            Assert.True(closed);
        }

        [Fact]
        public void WithExtension_SetsExtensionData()
        {
            NotificationRequest request = NotificationBuilder.Create()
                .WithExtension("key1", "value1")
                .WithExtension("key2", 42)
                .Build();

            Assert.NotNull(request.Extensions);
            Assert.Equal("value1", request.Extensions["key1"]);
            Assert.Equal(42, request.Extensions["key2"]);
        }

        [Fact]
        public void Build_GeneratesUniqueIds()
        {
            NotificationRequest r1 = NotificationBuilder.Create().Build();
            NotificationRequest r2 = NotificationBuilder.Create().Build();

            Assert.NotEqual(r1.Id, r2.Id);
        }

        [Fact]
        public void FluentChain_SetsAllProperties()
        {
            NotificationRequest request = NotificationBuilder
                .Create("Title", "Message")
                .AsSuccess()
                .InArea("TopArea")
                .ExpiresInSeconds(10)
                .WithPriority(NotificationPriority.High)
                .WithBackground("#00FF00")
                .WithForeground("#FFFFFF")
                .CloseOnClick()
                .GroupAs("test-group")
                .Build();

            Assert.Equal("Title", request.Title);
            Assert.Equal("Message", request.Message);
            Assert.Equal(NotificationType.Success, request.Type);
            Assert.Equal("TopArea", request.AreaName);
            Assert.Equal(TimeSpan.FromSeconds(10), request.ExpirationTime);
            Assert.Equal(NotificationPriority.High, request.Priority);
            Assert.NotNull(request.BackgroundColor);
            Assert.NotNull(request.ForegroundColor);
            Assert.True(request.CloseOnClick);
            Assert.Equal("test-group", request.GroupKey);
        }
    }
}
