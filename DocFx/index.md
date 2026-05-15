# Notification.Wpf

Cross-platform toast notifications library for .NET. Messages, progress bars, Builder API, DI support, lifecycle events.

## Packages

### [Notification.Core](reference/Notification.Core.html)

Platform-agnostic core (netstandard2.0) — zero UI dependencies.

- [INotificationService](reference/Notification.Core.INotificationService.html) — main abstraction for showing notifications
- [NotificationBuilder](reference/Notification.Core.NotificationBuilder.html) — fluent builder API
- [INotificationEventService](reference/Notification.Core.INotificationEventService.html) — lifecycle event tracking
- [INotificationConfiguration](reference/Notification.Core.INotificationConfiguration.html) — configuration interface
- [NotificationColor](reference/Notification.Core.NotificationColor.html) — platform-independent color type

### [Notification.Wpf](reference/Notification.Wpf.html)

WPF implementation (net10/9/8/4.8-windows) — full UI: toasts, progress bars, custom content.

- [NotificationManager](reference/Notification.Wpf.NotificationManager.html) — WPF notification manager
- [NotificationArea](reference/Notification.Wpf.Controls.NotificationArea.html) — XAML control for in-window notifications
- [NotificationConstants](reference/Notification.Wpf.NotificationConstants.html) — static configuration (backward compatible)

### [Notification.Console](reference/Notification.Console.html)

Console implementation (netstandard2.0) — colored terminal output.

- [ConsoleNotificationManager](reference/Notification.Console.ConsoleNotificationManager.html) — console notification manager

### [Notification.Avalonia](reference/Notification.Avalonia.html)

Avalonia UI implementation (net8.0) — cross-platform desktop notifications via WindowNotificationManager.

- [AvaloniaNotificationManager](reference/Notification.Avalonia.AvaloniaNotificationManager.html) — Avalonia notification manager

## Quick Start

```csharp
// Cross-platform via DI
services.AddWpfNotifications(config =>
{
    config.DefaultExpirationTime = TimeSpan.FromSeconds(5);
    config.MessagePosition = NotificationPosition.BottomRight;
});

// Resolve and use
INotificationService service = provider.GetRequiredService<INotificationService>();

service.Show(NotificationBuilder
    .Create("Success", "Operation completed!")
    .AsSuccess()
    .ExpiresInSeconds(5)
    .WithPriority(NotificationPriority.High)
    .OnClick(() => Console.WriteLine("Clicked"))
    .Build());
```

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](Getting-Started.html) | Installation, DI registration, first notification |
| [Builder API](Builder-API.html) | Fluent builder reference with all methods |
| [Configuration](Configuration.html) | INotificationConfiguration and NotificationConstants |
| [Lifecycle Events](Lifecycle-Events.html) | Event tracking: Shown, Clicked, Closed, TimedOut, Dismissed |

- [API Reference](reference/Notification.Core.html) — Full API documentation
