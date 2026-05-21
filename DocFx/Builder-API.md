# Builder API

The Builder API works identically on all platforms via `NotificationBuilder`. It is the recommended
modern replacement for the long-parameter `Show(...)` overloads — see the [Migration Guide](Migration-Guide.md).

## Basic Usage

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

## Method Reference

### Factory Methods

| Method | Description |
|--------|-------------|
| `Create()` | Create empty builder |
| `Create(title, message)` | Create builder with title and message |

### Content

| Method | Description |
|--------|-------------|
| `WithTitle(string)` | Set notification title |
| `WithMessage(string)` | Set notification message |
| `OfType(NotificationType)` | Set notification type |
| `AsSuccess()` | Shortcut for `OfType(Success)` |
| `AsWarning()` | Shortcut for `OfType(Warning)` |
| `AsError()` | Shortcut for `OfType(Error)` |
| `AsInformation()` | Shortcut for `OfType(Information)` |

### Display

| Method | Description |
|--------|-------------|
| `InArea(string)` | Target specific NotificationArea (WPF) |
| `ExpiresIn(TimeSpan)` | Set expiration time |
| `ExpiresInSeconds(double)` | Set expiration in seconds |
| `NeverExpires()` | Notification stays until dismissed |
| `CloseOnClick(bool)` | Close when clicked (default: true) |
| `HideCloseButton()` | Hide the X button |

### Priority and Grouping

| Method | Description |
|--------|-------------|
| `WithPriority(NotificationPriority)` | Low / Normal / High / Critical |
| `GroupAs(string)` | Group key for queue deduplication |

### Colors

| Method | Description |
|--------|-------------|
| `WithBackground(NotificationColor)` | Background color |
| `WithBackground(string hex)` | Background from hex string |
| `WithForeground(NotificationColor)` | Foreground color |
| `WithForeground(string hex)` | Foreground from hex string |

### Text Settings

| Method | Description |
|--------|-------------|
| `WithTitleSettings(Action<NotificationTextSettings>)` | Configure title font |
| `WithMessageSettings(Action<NotificationTextSettings>)` | Configure message font |
| `WithTrimming(NotificationTextTrimType, uint rows)` | Text trimming |

### Buttons

| Method | Description |
|--------|-------------|
| `WithLeftButton(string, Action)` | Left button with text and callback |
| `WithRightButton(string, Action)` | Right button with text and callback |
| `WithOkCancel(Action onOk, Action onCancel)` | OK/Cancel button pair |

### Callbacks

| Method | Description |
|--------|-------------|
| `OnClick(Action)` | Click callback |
| `OnClose(Action)` | Close callback |
| `OnRightClick(Action)` | Right-click callback |

### Other

| Method | Description |
|--------|-------------|
| `WithIcon(object)` | Icon (platform-specific) |
| `WithContent(object)` | Arbitrary platform-specific content — replaces the `Show(object content, ...)` overload |
| `WithExtension(string key, object value)` | Platform-specific extensions |
| `Build()` | Create `NotificationRequest` |

## NotificationTextSettings

```csharp
service.Show(NotificationBuilder
    .Create("Styled", "Custom fonts and alignment")
    .AsInformation()
    .WithTitleSettings(s =>
    {
        s.FontFamily = "Segoe UI";
        s.FontSize = 18;
        s.FontWeight = NotificationFontWeight.Bold;
        s.FontStyle = NotificationFontStyle.Normal;
        s.TextAlignment = NotificationTextAlignment.Center;
    })
    .WithMessageSettings(s =>
    {
        s.FontSize = 14;
        s.Opacity = 0.8;
    })
    .Build());
```

## NotificationColor

```csharp
// Predefined colors
NotificationColor color = NotificationColor.LimeGreen;

// From hex
NotificationColor hex = NotificationColor.FromHex("#2196F3");

// From RGB
NotificationColor rgb = NotificationColor.FromRgb(33, 150, 243);

// Implicit from string
NotificationColor implicit = "#FF5722";
```

## Custom Content

`WithContent` displays an arbitrary platform-specific UI object (for example, a WPF control) instead
of the standard title/message layout. It replaces the legacy `Show(object content, ...)` overload.

```csharp
StackPanel panel = new StackPanel();
panel.Children.Add(new TextBlock { Text = "Custom UI" });
panel.Children.Add(new ProgressBar { IsIndeterminate = true });

service.Show(NotificationBuilder
    .Create()
    .WithContent(panel)
    .NeverExpires()
    .Build());
```

When `WithContent` is set, the title, message and related text options are ignored.
