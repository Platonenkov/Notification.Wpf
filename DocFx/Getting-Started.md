# Getting Started

## Installation

```
// WPF (includes Core automatically)
Install-Package Notification.Wpf

// Or use Core directly for cross-platform
Install-Package Notification.CoreUI

// Platform-specific
Install-Package Notification.Console
Install-Package Notification.Avalonia
Install-Package Notification.Maui
```

## DI Registration

### WPF

```csharp
services.AddWpfNotifications(config =>
{
    config.DefaultExpirationTime = TimeSpan.FromSeconds(5);
    config.MessagePosition = NotificationPosition.BottomRight;
});
```

<details>
<summary><b>Full App.xaml.cs example</b></summary>

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

### Console

```csharp
services.AddConsoleNotifications();
```

### Avalonia

```csharp
var services = new ServiceCollection();
services.AddAvaloniaNotifications();
var provider = services.BuildServiceProvider();

var manager = provider.GetRequiredService<INotificationService>() as AvaloniaNotificationManager;
manager?.SetHost(this); // pass TopLevel
```

### MAUI

```csharp
// In MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    MauiAppBuilder builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseMauiCommunityToolkit(options =>
        {
#if WINDOWS
            options.SetShouldEnableSnackbarOnWindows(true);
#endif
        })
        .UseMauiNotifications(config =>
        {
            config.DefaultExpirationTime = TimeSpan.FromSeconds(3);
        });

    return builder.Build();
}
```

## WPF — Static API (no DI)

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

## First Notification

### Builder API (cross-platform)

```csharp
INotificationService service = /* from DI */;

service.Show(NotificationBuilder
    .Create("Title", "Message")
    .AsSuccess()
    .Build());
```

### WPF Classic API

```csharp
var notificationManager = new NotificationManager();
notificationManager.Show("Title", "Message", NotificationType.Success);
```

### In-Window Notifications (WPF)

XAML:
```xml
xmlns:notifications="clr-namespace:Notification.Wpf.Controls;assembly=Notification.Wpf"

<notifications:NotificationArea x:Name="WindowArea" Position="TopLeft" MaxItems="3"/>
```

Code:
```csharp
notificationManager.Show("Title", "Message", NotificationType.Warning, "WindowArea");
```

## Progress Bar

```csharp
using var progress = notificationManager.ShowProgressBar("Processing...", showCancelButton: true);

for (var i = 0; i <= 100; i++)
{
    progress.Cancel.ThrowIfCancellationRequested();
    progress.Report(new NotificationProgressReport(i, $"Step {i}", "With progress", true));
    await Task.Delay(TimeSpan.FromSeconds(0.02), progress.Cancel).ConfigureAwait(false);
}
```
