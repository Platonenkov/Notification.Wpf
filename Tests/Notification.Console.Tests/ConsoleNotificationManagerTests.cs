using System;
using System.IO;
using Notification.Core;
using Xunit;

namespace Notification.Console.Tests
{
    public class ConsoleNotificationManagerTests
    {
        [Fact]
        public void Show_ReturnsValidGuid()
        {
            ConsoleNotificationManager manager = new ConsoleNotificationManager();
            NotificationRequest request = NotificationBuilder.Create("Test", "Message").AsSuccess().Build();

            Guid id = manager.Show(request);

            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Show_WritesToConsole()
        {
            ConsoleNotificationManager manager = new ConsoleNotificationManager();
            NotificationRequest request = NotificationBuilder.Create("Test", "Hello World").AsSuccess().Build();

            StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);
            try
            {
                manager.Show(request);
                string output = writer.ToString();
                Assert.Contains("Test", output);
                Assert.Contains("Hello World", output);
            }
            finally
            {
                System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Theory]
        [InlineData(NotificationType.Success)]
        [InlineData(NotificationType.Warning)]
        [InlineData(NotificationType.Error)]
        [InlineData(NotificationType.Information)]
        public void Show_AllTypes_DoNotThrow(NotificationType type)
        {
            ConsoleNotificationManager manager = new ConsoleNotificationManager();
            NotificationRequest request = NotificationBuilder.Create("T", "M").OfType(type).Build();

            StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);
            try
            {
                Exception? ex = Record.Exception(() => manager.Show(request));
                Assert.Null(ex);
            }
            finally
            {
                System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Fact]
        public void Dismiss_RemovesActiveNotification()
        {
            ConsoleNotificationManager manager = new ConsoleNotificationManager();
            NotificationRequest request = NotificationBuilder.Create("T", "M").Build();

            StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);
            try
            {
                Guid id = manager.Show(request);
                Exception? ex = Record.Exception(() => manager.Dismiss(id));
                Assert.Null(ex);
            }
            finally
            {
                System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Fact]
        public void DismissAll_DoesNotThrow()
        {
            ConsoleNotificationManager manager = new ConsoleNotificationManager();

            StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);
            try
            {
                manager.Show(NotificationBuilder.Create("A", "1").Build());
                manager.Show(NotificationBuilder.Create("B", "2").Build());

                Exception? ex = Record.Exception(() => manager.DismissAll());
                Assert.Null(ex);
            }
            finally
            {
                System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Fact]
        public void Dismiss_UnknownId_DoesNotThrow()
        {
            ConsoleNotificationManager manager = new ConsoleNotificationManager();

            Exception? ex = Record.Exception(() => manager.Dismiss(Guid.NewGuid()));

            Assert.Null(ex);
        }

        [Fact]
        public void Show_WithEvents_FiresLifecycleEvent()
        {
            NotificationEventService events = new NotificationEventService();
            NotificationConfiguration config = new NotificationConfiguration();
            ConsoleNotificationManager manager = new ConsoleNotificationManager(config, events);
            bool eventFired = false;

            events.NotificationShown += (s, e) => eventFired = true;

            StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);
            try
            {
                manager.Show(NotificationBuilder.Create("T", "M").Build());
                Assert.True(eventFired);
            }
            finally
            {
                System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Fact]
        public void Show_InvokesOnClickCallback()
        {
            ConsoleNotificationManager manager = new ConsoleNotificationManager();
            bool clicked = false;

            NotificationRequest request = NotificationBuilder.Create("T", "M")
                .OnClick(() => clicked = true)
                .Build();

            StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);
            try
            {
                manager.Show(request);
            }
            finally
            {
                System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Fact]
        public void ImplementsINotificationService()
        {
            ConsoleNotificationManager manager = new ConsoleNotificationManager();

            Assert.IsAssignableFrom<INotificationService>(manager);
        }
    }
}
