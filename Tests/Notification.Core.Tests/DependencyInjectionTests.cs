using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Notification.Core.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddNotifications_RegistersConfiguration()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddNotifications();
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationConfiguration config = provider.GetRequiredService<INotificationConfiguration>();

            Assert.NotNull(config);
            Assert.IsType<NotificationConfiguration>(config);
        }

        [Fact]
        public void AddNotifications_RegistersEventService()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddNotifications();
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationEventService events = provider.GetRequiredService<INotificationEventService>();

            Assert.NotNull(events);
            Assert.IsType<NotificationEventService>(events);
        }

        [Fact]
        public void AddNotifications_RegistersQueue()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddNotifications();
            services.AddSingleton<INotificationService, FakeNotificationService>();
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationQueue queue = provider.GetRequiredService<INotificationQueue>();

            Assert.NotNull(queue);
        }

        [Fact]
        public void AddNotifications_WithConfigure_AppliesConfiguration()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddNotifications(config =>
            {
                config.DefaultExpirationTime = TimeSpan.FromSeconds(15);
                config.FontName = "Arial";
                config.MessagePosition = NotificationPosition.TopLeft;
            });
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationConfiguration config = provider.GetRequiredService<INotificationConfiguration>();

            Assert.Equal(TimeSpan.FromSeconds(15), config.DefaultExpirationTime);
            Assert.Equal("Arial", config.FontName);
            Assert.Equal(NotificationPosition.TopLeft, config.MessagePosition);
        }

        [Fact]
        public void AddNotifications_ConfigurationIsSingleton()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddNotifications();
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationConfiguration config1 = provider.GetRequiredService<INotificationConfiguration>();
            INotificationConfiguration config2 = provider.GetRequiredService<INotificationConfiguration>();

            Assert.Same(config1, config2);
        }

        [Fact]
        public void AddNotifications_EventServiceIsSingleton()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddNotifications();
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationEventService events1 = provider.GetRequiredService<INotificationEventService>();
            INotificationEventService events2 = provider.GetRequiredService<INotificationEventService>();

            Assert.Same(events1, events2);
        }
    }
}
