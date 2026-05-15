# Notification.Wpf

Cross-platform toast notifications library for .NET. Messages, progress bars, Builder API, DI support, lifecycle events.

## Packages

| Package | Target | Description |
|---------|--------|-------------|
| **Notification.Core** | `netstandard2.0` | Platform-agnostic core: interfaces, Builder API, events, queue, DI |
| **Notification.Wpf** | `net10/9/8/4.8-windows` | WPF implementation with full UI: toasts, progress bars, custom content |
| **Notification.Console** | `netstandard2.0` | Console implementation with colored terminal output |
| **Notification.Avalonia** | `net8.0` | Avalonia UI implementation via WindowNotificationManager |
| **Notification.Maui** | `net10.0-*` | .NET MAUI implementation via CommunityToolkit Snackbar |

## Installation

```
// WPF (includes Core automatically)
Install-Package Notification.Wpf

// Or use Core directly for cross-platform
Install-Package Notification.Core

// Platform-specific
Install-Package Notification.Console
Install-Package Notification.Avalonia
Install-Package Notification.Maui
```

## Quick Start — Builder API (cross-platform)

```csharp
// Works on any platform via INotificationService
INotificationService service = provider.GetRequiredService<INotificationService>();

service.Show(NotificationBuilder
    .Create("Success", "Operation completed!")
    .AsSuccess()
    .ExpiresInSeconds(5)
    .WithPriority(NotificationPriority.High)
    .OnClick(() => Console.WriteLine("Clicked"))
    .Build());
```

## DI Registration

```csharp
// WPF
services.AddWpfNotifications(config =>
{
    config.DefaultExpirationTime = TimeSpan.FromSeconds(5);
    config.MessagePosition = NotificationPosition.BottomRight;
});

// Console
services.AddConsoleNotifications();

// Avalonia
services.AddAvaloniaNotifications();

// MAUI (in MauiProgram.cs)
builder.UseMauiNotifications(config =>
{
    config.DefaultExpirationTime = TimeSpan.FromSeconds(3);
});
```

## WPF — Classic API (backward compatible)

All existing WPF APIs continue to work without changes:

```csharp
var notificationManager = new NotificationManager();

// Simple message
notificationManager.Show("Title", "Message", NotificationType.Success);

// Inside application window
notificationManager.Show("Title", "Message", NotificationType.Warning, "WindowArea");
```

### Notification Types

```
None, Information, Success, Warning, Error, Notification
```

### XAML — NotificationArea

```xml
xmlns:notifications="clr-namespace:Notification.Wpf.Controls;assembly=Notification.Wpf"

<notifications:NotificationArea x:Name="WindowArea" Position="TopLeft" MaxItems="3"/>
```

### Progress Bar

```csharp
using var progress = notificationManager.ShowProgressBar("Processing...", showCancelButton: true);

for (var i = 0; i <= 100; i++)
{
    progress.Cancel.ThrowIfCancellationRequested();
    progress.Report(new NotificationProgressReport(i, $"Step {i}", "With progress", true));
    await Task.Delay(TimeSpan.FromSeconds(0.02), progress.Cancel).ConfigureAwait(false);
}
```

## Lifecycle Events

```csharp
INotificationEventService events = provider.GetRequiredService<INotificationEventService>();
events.NotificationShown += (s, e) => Log($"Shown: {e.Title}");
events.NotificationClicked += (s, e) => Log($"Clicked: {e.NotificationId}");
events.NotificationClosed += (s, e) => Log($"Closed: {e.Stage}");
```

## Demo

![Notifications](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Files/notification.gif?raw=true)
![Progress](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Files/progress.gif?raw=true)
![Buttons](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Files/info_button.gif?raw=true)
![Styles](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Files/all_styles.gif?raw=true)

## Known Issues

**Notification window stays open after closing the app:**

```csharp
Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
```

**Using in non-WPF hosts (WinForms, Console, VSTO, tests):**

Since v9.0, `Application.Current` null-checks are built-in. The library works correctly when hosted in WinForms, console applications, Office add-ins, and other non-WPF processes without additional configuration.

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](Getting-Started.md) | Installation, DI registration, first notification |
| [Builder API](Builder-API.md) | Fluent builder reference with all methods |
| [Configuration](Configuration.md) | INotificationConfiguration and NotificationConstants |
| [Lifecycle Events](Lifecycle-Events.md) | Event tracking: Shown, Clicked, Closed, TimedOut, Dismissed |
| API Reference | Full API documentation (see navbar) |
