# Configuration

## INotificationConfiguration (cross-platform)

Configure via DI registration:

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

## NotificationConstants (WPF static, backward compatible)

All static properties delegate to the underlying `INotificationConfiguration`:

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

## Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MessagePosition` | `NotificationPosition` | `BottomRight` | Toast position on screen |
| `IsReversedPanel` | `bool` | `false` | Reverse stack order |
| `MinWidth` | `double` | `350` | Minimum notification width |
| `MaxWidth` | `double` | `350` | Maximum notification width |
| `MaxOverlayWindowCount` | `int` | `999` | Max visible notifications |
| `DefaultExpirationTime` | `TimeSpan` | `5 seconds` | Auto-close time |
| `CollapseProgressIfMoreRows` | `bool` | `true` | Collapse progress when many items |
| `SuccessBackgroundColor` | `NotificationColor` | `LimeGreen` | Success background |
| `WarningBackgroundColor` | `NotificationColor` | `Orange` | Warning background |
| `ErrorBackgroundColor` | `NotificationColor` | `OrangeRed` | Error background |
| `InformationBackgroundColor` | `NotificationColor` | `CornflowerBlue` | Information background |
| `DefaultForegroundColor` | `NotificationColor` | `WhiteSmoke` | Default text color |
| `FontName` | `string` | `"Segoe UI"` | Font family |
| `BaseTextSize` | `double` | `14` | Base font size |
| `TitleSize` | `double` | `14` | Title font size |
| `MessageSize` | `double` | `14` | Message font size |
| `DefaultRowCount` | `uint` | `2` | Max text rows |
| `DefaultTrimType` | `NotificationTextTrimType` | `NoTrim` | Text trimming |
