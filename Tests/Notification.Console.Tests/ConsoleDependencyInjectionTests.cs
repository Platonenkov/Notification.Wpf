using System;
using Microsoft.Extensions.DependencyInjection;
using Notification.Core;
using Xunit;

namespace Notification.Console.Tests
{
    public class ConsoleDependencyInjectionTests
    {
        [Fact]
        public void AddConsoleNotifications_RegistersINotificationService()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddConsoleNotifications();
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationService service = provider.GetRequiredService<INotificationService>();

            Assert.NotNull(service);
            Assert.IsType<ConsoleNotificationManager>(service);
        }

        [Fact]
        public void AddConsoleNotifications_RegistersCoreServices()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddConsoleNotifications();
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationConfiguration config = provider.GetRequiredService<INotificationConfiguration>();
            INotificationEventService events = provider.GetRequiredService<INotificationEventService>();

            Assert.NotNull(config);
            Assert.NotNull(events);
        }

        [Fact]
        public void AddConsoleNotifications_WithConfigure_AppliesConfig()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddConsoleNotifications(config =>
            {
                config.DefaultExpirationTime = TimeSpan.FromSeconds(20);
            });
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationConfiguration config = provider.GetRequiredService<INotificationConfiguration>();

            Assert.Equal(TimeSpan.FromSeconds(20), config.DefaultExpirationTime);
        }

        [Fact]
        public void AddConsoleNotifications_ServiceIsSingleton()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddConsoleNotifications();
            ServiceProvider provider = services.BuildServiceProvider();

            INotificationService s1 = provider.GetRequiredService<INotificationService>();
            INotificationService s2 = provider.GetRequiredService<INotificationService>();

            Assert.Same(s1, s2);
        }
    }
}
