<details open>
  <br />
  <summary><h2>🔄 Migration Guide (read this first if upgrading)</h2></summary>

Version **10.0** is **fully backward compatible** — existing code keeps compiling and running.
Two legacy APIs are now marked `[Obsolete]` (still functional, but the compiler warns). Migrate for
cleaner call sites and to remove the warnings.

| Legacy API | Modern API | Status |
|-----------|-----------|--------|
| `progress.GetProgress<(double,string)>(...)` + tuple `Report` | `progress.Report(value, message)` | ⚠️ `[Obsolete]` |
| `ShowProgressBar(...)` — 16 positional parameters | `ShowProgressBar(ProgressBarOptions)` | ⚠️ `[Obsolete]` |
| `Show(...)` — up to 18 positional parameters | `NotificationBuilder` | ✅ still supported, Builder recommended |

### Progress reporting — tuples → `Report(...)` overloads

```csharp
// Before: fake-generic, tuple-typed
IProgress<(double, string)> p = progress.GetProgress<(double, string)>(showCancel: true);
p.Report((50.0, "half done"));

// After: plain Report overloads
progress.Report(50, "half done");                  // value + message
progress.Report(50, "half done", "Working", true);  // + title + cancel-button flag
progress.ReportIndeterminate("Waiting...");         // no numeric value
```

### `ShowProgressBar` — 16 parameters → `ProgressBarOptions`

```csharp
// Before: 16 positional parameters
using var progress = manager.ShowProgressBar(
    "Processing files", true, true, "MainArea", false, 1, "Calculation time",
    true, true, Brushes.DarkSlateGray, Brushes.White, Brushes.LimeGreen, null, null, null, true);

// After: a configurable options object — set only what you need
using var progress = manager.ShowProgressBar(new ProgressBarOptions
{
    Title              = "Processing files",
    AreaName           = "MainArea",
    IsCollapse         = true,
    BackgroundColor    = NotificationColor.FromHex("#2F4F4F"),
    ProgressColor      = NotificationColor.LimeGreen,
});
```

### `Show` — long parameter lists → `NotificationBuilder`

```csharp
// Before: up to 18 positional parameters with many `null`s
manager.Show("Update available", "Version 2.0 is ready", NotificationType.Information,
    "MainArea", TimeSpan.FromSeconds(15), null, null, () => Install(), "Install",
    () => { }, "Later", NotificationTextTrimType.Trim, 3, true, null, null, true, null);

// After: named, chainable Builder methods
manager.Show(NotificationBuilder
    .Create("Update available", "Version 2.0 is ready")
    .AsInformation()
    .InArea("MainArea")
    .ExpiresInSeconds(15)
    .WithLeftButton("Install", () => Install())
    .WithRightButton("Later", () => { })
    .WithTrimming(NotificationTextTrimType.Trim, 3)
    .Build());

// Arbitrary custom content: Show(object content, ...)  ->  WithContent(...)
manager.Show(NotificationBuilder.Create().WithContent(myCustomGrid).NeverExpires().Build());
```

➡️ **Full step-by-step guide with complete before/after examples, parameter-mapping tables and links
to working Sample code: [Migration.md](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Migration.md)**

</details>

<details>
  <br />
  <summary><h2>Initialization</h2></summary>

### Cross-platform (via Notification.Core)

Any platform using `INotificationService`:

```csharp
// Register in DI container
services.AddNotifications(config =>
{
    config.DefaultExpirationTime = TimeSpan.FromSeconds(5);
    config.MessagePosition = NotificationPosition.BottomRight;
});

// Then register a platform-specific implementation:
// WPF:
services.AddWpfNotifications();
// Console:
services.AddConsoleNotifications();
// Avalonia:
services.AddAvaloniaNotifications();
```

### WPF — method 1 (DI)

```csharp
services.AddWpfNotifications(config =>
{
    config.DefaultExpirationTime = TimeSpan.FromSeconds(5);
    config.SuccessBackgroundColor = NotificationColor.LimeGreen;
});
```

<details>
  <br />
  <summary><b>Full code</b></summary>

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed partial class App
{
    public static IServiceProvider Services => Hosting.Services;
    private static IHost __Hosting;

    public static IHost Hosting => __Hosting ??=
        CreateHostBuilder(Environment.GetCommandLineArgs()).Build();

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
       .CreateDefaultBuilder(args)
       .ConfigureServices(ConfigureServices);

    private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
    {
        services.AddWpfNotifications(config =>
        {
            config.DefaultExpirationTime = TimeSpan.FromSeconds(5);
        });
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        var host = Hosting;
        base.OnStartup(e);
        await host.StartAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        using var host = Hosting;
        await host.StopAsync();
    }
}
```

</details>

### WPF — method 2 (static)

```csharp
public static class Notifier
{
    private static readonly NotificationManager _manager = new();

    public static void Show(string title, string message, NotificationType type)
    {
        _manager.Show(title, message, type);
    }
}
```

### MAUI

```csharp
// In MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    MauiAppBuilder builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseMauiCommunityToolkit()    // required by Notification.Maui
        .UseMauiNotifications(config =>
        {
            config.DefaultExpirationTime = TimeSpan.FromSeconds(3);
        });

    return builder.Build();
}
```

> **Windows Snackbar:** CommunityToolkit.Maui Snackbar on Windows uses native `AppNotification`
> which requires COM server registration. If you need Snackbar (action buttons) on Windows,
> add `options.SetShouldEnableSnackbarOnWindows(true)` inside `.UseMauiCommunityToolkit(options => { ... })`.
> Note: this may cause "No COM servers are registered for this app" in Blazor Hybrid or
> unpackaged apps. Without this option, simple Toast notifications still work on all platforms.

### Avalonia

```csharp
// In MainWindow constructor or OnLoaded
var services = new ServiceCollection();
services.AddAvaloniaNotifications();
var provider = services.BuildServiceProvider();

var manager = provider.GetRequiredService<INotificationService>() as AvaloniaNotificationManager;
manager?.SetHost(this); // pass TopLevel
```

</details>

<details>
  <br />
  <summary><h2>Builder API (cross-platform)</h2></summary>

The Builder API works identically on all platforms:

```csharp
INotificationService service = /* from DI */;

// Simple notification
service.Show(NotificationBuilder
    .Create("Title", "Message")
    .AsSuccess()
    .Build());

// Full-featured notification
service.Show(NotificationBuilder
    .Create()
    .WithTitle("Download Complete")
    .WithMessage("File saved to ~/Downloads")
    .AsInformation()
    .ExpiresInSeconds(10)
    .WithPriority(NotificationPriority.High)
    .GroupAs("downloads")
    .WithBackground("#2196F3")
    .WithForeground("#FFFFFF")
    .WithTitleSettings(s =>
    {
        s.FontSize = 18;
        s.FontWeight = NotificationFontWeight.Bold;
    })
    .WithLeftButton("Open", () => OpenFile())
    .WithRightButton("Dismiss", () => { })
    .OnClick(() => OpenFolder())
    .OnClose(() => LogClosed())
    .CloseOnClick()
    .Build());

// Extension method with configure action
service.Show(b => b
    .WithTitle("Quick")
    .WithMessage("One-liner")
    .AsWarning());
```

### Builder Methods

| Method | Description |
|--------|-------------|
| `Create()` / `Create(title, message)` | Factory methods |
| `WithTitle(string)` | Set title |
| `WithMessage(string)` | Set message |
| `OfType(NotificationType)` | Set notification type |
| `AsSuccess()` / `AsWarning()` / `AsError()` / `AsInformation()` | Type shortcuts |
| `InArea(string)` | Target specific NotificationArea (WPF) |
| `ExpiresIn(TimeSpan)` / `ExpiresInSeconds(double)` | Set expiration |
| `NeverExpires()` | Notification stays until dismissed |
| `CloseOnClick(bool)` | Close on click (default: true) |
| `HideCloseButton()` | Hide the X button |
| `WithPriority(NotificationPriority)` | Low / Normal / High / Critical |
| `GroupAs(string)` | Group key for queue deduplication |
| `WithBackground(NotificationColor)` / `WithBackground(string hex)` | Background color |
| `WithForeground(NotificationColor)` / `WithForeground(string hex)` | Foreground color |
| `WithTitleSettings(Action<NotificationTextSettings>)` | Configure title font |
| `WithMessageSettings(Action<NotificationTextSettings>)` | Configure message font |
| `WithTrimming(NotificationTextTrimType, uint rows)` | Text trimming |
| `WithLeftButton(string, Action)` | Left button |
| `WithRightButton(string, Action)` | Right button |
| `WithOkCancel(Action onOk, Action onCancel)` | OK/Cancel buttons |
| `OnClick(Action)` | Click callback |
| `OnClose(Action)` | Close callback |
| `OnRightClick(Action)` | Right-click callback |
| `WithIcon(object)` | Icon (platform-specific) |
| `WithContent(object)` | Arbitrary platform-specific content — replaces the `Show(object content, ...)` overload |
| `WithExtension(string key, object value)` | Platform-specific extensions |
| `Build()` | Create `NotificationRequest` |

</details>

<details>
  <br />
  <summary><h2>Lifecycle Events</h2></summary>

Subscribe to notification lifecycle events via `INotificationEventService`:

```csharp
INotificationEventService events = provider.GetRequiredService<INotificationEventService>();

// All lifecycle changes
events.NotificationLifecycleChanged += (sender, e) =>
{
    Console.WriteLine($"[{e.Stage}] {e.Title} (ID: {e.NotificationId})");
};

// Specific events
events.NotificationShown += (s, e) => Log($"Shown: {e.Title}");
events.NotificationClicked += (s, e) => Log($"Clicked: {e.NotificationId}");
events.NotificationClosed += (s, e) => Log($"Closed: {e.Stage}"); // Closed, TimedOut, Dismissed
```

### Lifecycle Stages

| Stage | Description |
|-------|-------------|
| `Shown` | Notification displayed |
| `Clicked` | User clicked notification |
| `Closed` | Closed normally (by button or expiration) |
| `TimedOut` | Expired automatically |
| `Dismissed` | Dismissed programmatically via `Dismiss()` |

</details>

<details>
  <br />
  <summary><h2>Notifications (WPF classic API)</h2></summary>

All existing WPF APIs remain fully backward compatible:

```csharp
var notificationManager = new NotificationManager();
notificationManager.Show("Title", "Message", NotificationType.Success);
```

[Message initialization methods](https://github.com/Platonenkov/Notification.Wpf/blob/dev/src/Notification.Wpf/Base/Interfaces/Base/IMessageManager.cs)

> **Recommended:** use the Builder API instead of the long-parameter `Show(...)` overloads. See the [Migration Guide](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Migration.md).

</details>

<details>
  <br />
  <summary><h2>Progress Bar</h2></summary>

```csharp
using var progress = notificationManager.ShowProgressBar(new ProgressBarOptions
{
    Title            = "Processing...",
    ShowCancelButton = true,
});

for (var i = 0; i <= 100; i++)
{
    progress.Cancel.ThrowIfCancellationRequested();
    progress.Report(i, $"Step {i}", "With progress");   // tuple-free Report overload
    await Task.Delay(TimeSpan.FromSeconds(0.02), progress.Cancel).ConfigureAwait(false);
}
```

### `Report(...)` overloads

| Call | Effect |
|------|--------|
| `progress.Report(value)` | Update progress value only |
| `progress.Report(value, message)` | Value + status message |
| `progress.Report(value, message, title)` | Value + message + title |
| `progress.Report(value, message, title, showCancel)` | + cancel-button visibility |
| `progress.ReportIndeterminate(message, title)` | Indeterminate progress (no numeric value) |

`NotificationProgressReport` fields (the underlying struct):
- `Value` (double?) — progress percentage (0-100), null hides the bar
- `Message` (string) — status message
- `Title` (string) — progress title
- `ShowCancel` (bool?) — show/hide cancel button

> The 16-parameter `ShowProgressBar(...)` overload and the tuple-based `GetProgress<T>` API are
> `[Obsolete]`. See the [Migration Guide](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Migration.md).

[Progress initialization methods](https://github.com/Platonenkov/Notification.Wpf/blob/dev/src/Notification.Wpf/Base/Interfaces/Base/IProgressManager.cs)

</details>

<details>
  <br />
  <summary><h2>Configuration</h2></summary>

### Via INotificationConfiguration (cross-platform)

```csharp
services.AddNotifications(config =>
{
    // Position & layout
    config.MessagePosition = NotificationPosition.BottomRight;
    config.IsReversedPanel = false;
    config.MinWidth = 350.0;
    config.MaxWidth = 350.0;
    config.MaxOverlayWindowCount = 999;
    config.CollapseProgressIfMoreRows = true;

    // Timing
    config.DefaultExpirationTime = TimeSpan.FromSeconds(5);

    // Colors (platform-independent NotificationColor)
    config.SuccessBackgroundColor = NotificationColor.LimeGreen;
    config.WarningBackgroundColor = NotificationColor.Orange;
    config.ErrorBackgroundColor = NotificationColor.OrangeRed;
    config.InformationBackgroundColor = NotificationColor.CornflowerBlue;
    config.DefaultForegroundColor = NotificationColor.WhiteSmoke;

    // Text
    config.FontName = "Segoe UI";
    config.BaseTextSize = 14.0;
    config.TitleSize = 14.0;
    config.MessageSize = 14.0;
    config.TitleTextAlignment = NotificationTextAlignment.Left;
    config.MessageTextAlignment = NotificationTextAlignment.Left;

    // Trimming
    config.DefaultRowCount = 2;
    config.DefaultTrimType = NotificationTextTrimType.NoTrim;

    // Button labels
    config.DefaultLeftButtonContent = "Ok";
    config.DefaultRightButtonContent = "Cancel";
    config.DefaultProgressButtonContent = "Cancel";
    config.CancellationMessage = "Operation was cancelled";
});
```

### Via NotificationConstants (WPF static, backward compatible)

```csharp
NotificationConstants.FontName = "Segoe UI";
NotificationConstants.MessageSize = 14;
NotificationConstants.TitleSize = 14;
NotificationConstants.MessageTextAlignment = TextAlignment.Left;
NotificationConstants.TitleTextAlignment = TextAlignment.Left;

NotificationConstants.SuccessBackgroundColor = new SolidColorBrush(Colors.LimeGreen);
NotificationConstants.WarningBackgroundColor = new SolidColorBrush(Colors.Orange);
NotificationConstants.ErrorBackgroundColor = new SolidColorBrush(Colors.OrangeRed);
NotificationConstants.InformationBackgroundColor = new SolidColorBrush(Colors.CornflowerBlue);

NotificationConstants.MessagePosition = NotificationPosition.BottomRight;
NotificationConstants.MinWidth = 350D;
NotificationConstants.MaxWidth = 350D;
```

### WPF behavior settings (NotificationConstants only)

WPF-only static settings that are **not** part of `INotificationConfiguration`. Set them once before
showing notifications — all are opt-in and the defaults preserve the previous behavior.

```csharp
// Keep the notification open while the cursor is over it — the auto-close timer
// is paused on mouse-over and resumed on mouse-leave (issue #71). Default: false.
NotificationConstants.KeepNotificationVisibleOnMouseOver = true;

// Whether the toast overlay window stays on top of other windows (issue #65). Default: true.
NotificationConstants.OverlayWindowTopmost = false;

// Rounded corners for the notification card (issue #52). Default: new CornerRadius(0).
NotificationConstants.NotificationCornerRadius = new CornerRadius(8);
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `KeepNotificationVisibleOnMouseOver` | `bool` | `false` | Pause the auto-close timer while the mouse is over the notification |
| `OverlayWindowTopmost` | `bool` | `true` | Whether the toast overlay window stays on top of other windows |
| `NotificationCornerRadius` | `CornerRadius` | `0` (square) | Corner radius of the notification card |

`NotificationCornerRadius` requires `using System.Windows;` and is also exposed per notification
as the `Notification.CornerRadius` dependency property.

### Right-click action

```csharp
// Builder API
service.Show(NotificationBuilder
    .Create("Title", "Message")
    .OnRightClick(() => ShowContextMenu())
    .Build());

// Classic WPF — via NotificationContent
var content = new NotificationContent
{
    Title = "Title",
    Message = "Message",
    RightClickAction = () => ShowContextMenu(),
};
```

</details>

<details>
  <br />
  <summary><h2>Known Issues</h2></summary>

### WPF — Notification window stays open after closing the app

```csharp
Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
```

### WPF — Using in non-WPF hosts (WinForms, Console, VSTO, tests)

Since v9.0, `Application.Current` null-checks are built-in. The library works correctly when hosted in WinForms, console applications, Office add-ins, and other non-WPF processes without additional configuration.

### MAUI on Windows — notification appearance

On Windows, CommunityToolkit.Maui Snackbar uses native `AppNotification` (toast). The app name and icon shown in the notification center are determined by Windows app registration, not by the Snackbar API. `SnackbarOptions` colors are also ignored on Windows — the library uses emoji prefixes (✅ ⚠️ ❌ ℹ️) to visually distinguish notification types. Action buttons require `Snackbar`; notifications without buttons use `Toast` to avoid an empty button area.

### MAUI — platform limitations

CommunityToolkit.Maui Snackbar has hard API limitations that cannot be worked around:

| Feature | Support | Notes |
|---------|---------|-------|
| Text (title + message) | ✅ Yes | Combined as single string with emoji prefix |
| Single action button | ✅ Yes | Only `LeftButtonContent` / `LeftButtonAction` is used |
| Multiple buttons | ❌ No | Snackbar API accepts exactly one action. `RightButtonContent` is ignored |
| Images | ❌ No | Snackbar has no image parameter. `PlatformImage` / `Icon` are ignored |
| Custom content | ❌ No | Snackbar is a system component, no arbitrary UI |
| Progress bar | ❌ No | Not supported by Snackbar/Toast API |
| Colors (Android/iOS/Mac) | ✅ Yes | Background, text, action button colors via `SnackbarOptions` |
| Colors (Windows) | ❌ No | Windows uses native AppNotification, ignores `SnackbarOptions` |

For full notification features (images, multiple buttons, progress bars, custom content), use the **WPF** or **Avalonia** implementations.

</details>
