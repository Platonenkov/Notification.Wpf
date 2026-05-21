# Notification.Wpf

Cross-platform toast notifications library for .NET. Messages, progress bars, Builder API, DI support, lifecycle events.

| [API Docs](https://platonenkov.github.io/Notification.Wpf/) | [Docs](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Documentation.md) | [Migration Guide](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Migration.md) | [Updates](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Updates.md) | [WPF Sample](https://github.com/Platonenkov/Notification.Wpf/tree/dev/Samples/Notification.Wpf.Sample) |
| --- | --- | --- | --- | --- |

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
service.Show(NotificationBuilder
    .Create("Success", "Operation completed!")
    .AsSuccess()
    .ExpiresInSeconds(5)
    .WithPriority(NotificationPriority.High)
    .OnClick(() => Console.WriteLine("Clicked"))
    .Build());
```

## Migrating from an earlier version?

Version 10.0 is **fully backward compatible** — existing code keeps working. Two legacy APIs are now
marked `[Obsolete]` and have clearer replacements:

```csharp
// Progress: tuple API  ->  Report(...) overloads
progress.Report(50, "half done", "Working");

// ShowProgressBar: 16 positional parameters  ->  ProgressBarOptions
manager.ShowProgressBar(new ProgressBarOptions { Title = "Working...", ProgressColor = NotificationColor.LimeGreen });

// Show: up to 18 positional parameters  ->  NotificationBuilder (recommended, the old overloads still work)
manager.Show(NotificationBuilder.Create("Title", "Message").AsSuccess().ExpiresInSeconds(5).Build());
```

➡️ **Full step-by-step guide with before/after examples and parameter mapping tables:
[Migration Guide](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Migration.md)**

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

## Lifecycle Events

```csharp
var events = serviceProvider.GetRequiredService<INotificationEventService>();
events.NotificationShown += (s, e) => Log($"Shown: {e.Title}");
events.NotificationClicked += (s, e) => Log($"Clicked: {e.NotificationId}");
events.NotificationClosed += (s, e) => Log($"Closed: {e.Stage}");
```

## WPF — Classic API (backward compatible)

All existing WPF APIs continue to work:

```csharp
var notificationManager = new NotificationManager();

// Simple message
notificationManager.Show("Title", "Message", NotificationType.Success);

// Inside application window
notificationManager.Show("Title", "Message", NotificationType.Warning, "WindowArea");

// Progress bar
using var progress = notificationManager.ShowProgressBar(new ProgressBarOptions { Title = "Processing..." });
for (var i = 0; i <= 100; i++)
{
    progress.Report(i, $"Step {i}");   // tuple-free Report overload
    await Task.Delay(30, progress.Cancel);
}
```

> See the [Migration Guide](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Migration.md) if you
> still use the 16-parameter `ShowProgressBar(...)` or the tuple-based progress API.

### Notification Types
```
None, Information, Success, Warning, Error, Notification
```

### XAML — NotificationArea
```xml
xmlns:notifications="clr-namespace:Notification.Wpf.Controls;assembly=Notification.Wpf"

<notifications:NotificationArea x:Name="WindowArea" Position="TopLeft" MaxItems="3"/>
```

![Demo](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Files/notification.gif)
![Demo](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Files/progress.gif)
![Demo](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Files/info_button.gif)
![Demo](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Files/all_styles.gif)

## Architecture

```
src/
  Notification.Core (netstandard2.0)         <- zero UI dependencies
    ├── Interfaces (INotificationService, INotificationConfiguration)
    ├── Builder API (NotificationBuilder -> NotificationRequest)
    ├── Events (INotificationEventService, lifecycle tracking)
    ├── Queue (INotificationQueue with priorities)
    └── DI extensions (AddNotifications)
  Notification.Wpf (net10/9/8/4.8-windows)  <- depends on Core
  Notification.Console (netstandard2.0)      <- depends on Core
  Notification.Avalonia (net8.0)             <- depends on Core
  Notification.Maui (net10.0-*)              <- depends on Core

Tests/
  Notification.Core.Tests
  Notification.Console.Tests
  Notification.Wpf.Tests

Samples/
  Notification.Wpf.Sample
  Notification.Console.Sample
  Notification.Avalonia.Sample
  Notification.Maui.Sample

DocFx/                                       <- DocFX API documentation
```

## Tests

```
dotnet test Tests/Notification.Core.Tests
dotnet test Tests/Notification.Console.Tests
dotnet test Tests/Notification.Wpf.Tests
```

134 unit tests covering Builder API, events, queue, configuration, adapters, and platform implementations.

## Known Issues

**Notification window stays open after closing the app:**
```csharp
Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
```

**Using in non-WPF hosts (WinForms, Console, VSTO, tests):**

Since v9.0, `Application.Current` null-checks are built-in. The library works correctly when hosted in WinForms, console applications, Office add-ins, and other non-WPF processes without additional configuration.

**MAUI on Windows — notification appearance:**

On Windows, CommunityToolkit.Maui Snackbar uses native `AppNotification` (toast). The app name and icon shown in the notification center are determined by Windows app registration, not by the Snackbar API. `SnackbarOptions` colors are also ignored on Windows — the library uses emoji prefixes (✅ ⚠️ ❌ ℹ️) to visually distinguish notification types. Action buttons require `Snackbar`; notifications without buttons use `Toast` to avoid an empty button area.

**MAUI — platform limitations:**

CommunityToolkit.Maui Snackbar does not support multiple buttons, images, custom content, or progress bars. Only one action button (`LeftButtonContent`) is supported; `RightButtonContent`, `PlatformImage`, and `Icon` are ignored. For full notification features, use the WPF or Avalonia implementations.

## Credits

This project was forked from https://github.com/Federerer/Notifications.Wpf

Project was re-released due to lack of owner interest in updating it.
