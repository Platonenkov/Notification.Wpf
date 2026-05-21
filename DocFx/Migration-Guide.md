# Migration Guide

Notification.Wpf **10.0** keeps **full backward compatibility** — all existing code keeps compiling
and running. This guide shows how to move to the modern, clearer APIs.

Two legacy APIs are now marked `[Obsolete]` (they still work, but the compiler emits a warning).
Migrating away from them removes the warnings and makes call sites far easier to read.

## Quick reference

| Legacy API | Modern API | Status |
|-----------|-----------|--------|
| `progress.GetProgress<(double,string)>(...)` + tuple `Report((v, msg))` | `progress.Report(value, message)` | ⚠️ `[Obsolete]` |
| `ShowProgressBar(...)` — 16 positional parameters | `ShowProgressBar(ProgressBarOptions)` | ⚠️ `[Obsolete]` |
| `Show(...)` — up to 18 positional parameters | `NotificationBuilder` | ✅ still supported — Builder recommended |

> The long-parameter `Show(...)` overloads are **not** deprecated — they keep working without warnings.
> The [Builder API](Builder-API.md) is simply the recommended modern alternative.

---

## 1. Progress reporting — drop the tuples

The old API exposed a fake-generic `GetProgress<T>()` that only accepted six hard-coded types
(`double`, `int` and four tuple shapes) and threw `NotSupportedException` at runtime for anything else.

**Before:**

```csharp
using var progress = manager.ShowProgressBar("Working...");

IProgress<(double, string)> p = progress.GetProgress<(double, string)>(showCancel: true);
p.Report((50.0, "half done"));
```

**After:**

```csharp
using var progress = manager.ShowProgressBar(new ProgressBarOptions { Title = "Working..." });

progress.Report(50);                              // value only
progress.Report(50, "half done");                 // value + message
progress.Report(50, "half done", "Working");       // value + message + title
progress.Report(50, "half done", "Working", true); // + cancel-button visibility
progress.ReportIndeterminate("Waiting...");        // no numeric value
```

For a UI-agnostic calculation layer that must report plain numbers:

```csharp
IProgress<double> plain  = progress.GetValueProgress(showCancel: true, scale: true);
IProgress<NotificationProgressReport> slowed = progress.GetSlowedProgress(updateTimeOut: 100);
IProgress<double> slowedPlain = progress.GetSlowedValueProgress(updateTimeOut: 100);
```

📂 Working example: [`MainWindow.xaml.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Wpf.Sample/MainWindow.xaml.cs)

---

## 2. `ShowProgressBar` — 16 parameters → `ProgressBarOptions`

The 16-parameter positional `ShowProgressBar(...)` overload is now `[Obsolete]`.

**Before:**

```csharp
using var progress = manager.ShowProgressBar(
    "Processing files", true, true, "MainArea", false, 1, "Calculation time",
    true, true, Brushes.DarkSlateGray, Brushes.White, Brushes.LimeGreen, null, null, null, true);
```

**After:**

```csharp
using var progress = manager.ShowProgressBar(new ProgressBarOptions
{
    Title              = "Processing files",
    ShowCancelButton   = true,
    AreaName           = "MainArea",
    IsCollapse         = true,
    BaseWaitingMessage = "Calculation time",
    BackgroundColor    = NotificationColor.FromHex("#2F4F4F"),
    ForegroundColor    = NotificationColor.White,
    ProgressColor      = NotificationColor.LimeGreen,
});
```

Set only the properties you need — every property has a sensible default.

### Parameter mapping

| Legacy parameter | `ProgressBarOptions` property | Type change |
|------------------|-------------------------------|-------------|
| `Title` | `Title` | — |
| `ShowCancelButton` | `ShowCancelButton` | — |
| `ShowProgress` | `ShowProgress` | — |
| `areaName` | `AreaName` | — |
| `TrimText` | `TrimText` | — |
| `DefaultRowsCount` | `DefaultRowsCount` | — |
| `BaseWaitingMessage` | `BaseWaitingMessage` | — |
| `IsCollapse` | `IsCollapse` | — |
| `TitleWhenCollapsed` | `TitleWhenCollapsed` | — |
| `background` | `BackgroundColor` | `Brush` → `NotificationColor?` |
| `foreground` | `ForegroundColor` | `Brush` → `NotificationColor?` |
| `progressColor` | `ProgressColor` | `Brush` → `NotificationColor?` |
| `icon` | `Icon` | — |
| `TitleSettings` | `TitleSettings` | `TextContentSettings` → `NotificationTextSettings` |
| `MessageSettings` | `MessageSettings` | `TextContentSettings` → `NotificationTextSettings` |
| `ShowXbtn` | `ShowCloseButton` | renamed |

> The `ShowProgressBar(ICustomizedNotification content, ...)` overload (custom-content progress) is
> **not** deprecated and keeps working unchanged.

---

## 3. `Show` — long parameter lists → `NotificationBuilder`

The heaviest `Show(...)` overload takes **18 positional parameters**. The [Builder API](Builder-API.md)
expresses the same notification with named, chainable methods. The legacy overloads still work — this
migration is recommended, not required.

**Before:**

```csharp
manager.Show("Update available", "Version 2.0 is ready", NotificationType.Information,
    "MainArea", TimeSpan.FromSeconds(15), () => Log("clicked"), () => Log("closed"),
    () => Install(), "Install", () => { }, "Later",
    NotificationTextTrimType.Trim, 3, true, null, null, true, null);
```

**After:**

```csharp
manager.Show(NotificationBuilder
    .Create("Update available", "Version 2.0 is ready")
    .AsInformation()
    .InArea("MainArea")
    .ExpiresInSeconds(15)
    .OnClick(() => Log("clicked"))
    .OnClose(() => Log("closed"))
    .WithLeftButton("Install", () => Install())
    .WithRightButton("Later", () => { })
    .WithTrimming(NotificationTextTrimType.Trim, 3)
    .Build());
```

### Parameter mapping

| Legacy parameter | Builder method |
|------------------|----------------|
| `title` | `Create(title, message)` or `WithTitle(title)` |
| `message` | `WithMessage(message)` |
| `type` | `OfType(type)` / `AsSuccess()` / `AsWarning()` / `AsError()` / `AsInformation()` |
| `areaName` | `InArea(areaName)` |
| `expirationTime` | `ExpiresIn(time)` / `ExpiresInSeconds(n)` / `NeverExpires()` |
| `onClick` | `OnClick(action)` |
| `onClose` | `OnClose(action)` |
| `LeftButton` + `LeftButtonText` | `WithLeftButton(text, action)` |
| `RightButton` + `RightButtonText` | `WithRightButton(text, action)` |
| `trim` + `RowsCountWhenTrim` | `WithTrimming(trimType, rows)` |
| `CloseOnClick` | `CloseOnClick(bool)` |
| `TitleSettings` | `WithTitleSettings(s => ...)` |
| `MessageSettings` | `WithMessageSettings(s => ...)` |
| `ShowXbtn` (`false`) | `HideCloseButton()` |
| `icon` | `WithIcon(icon)` |

### Custom content — `Show(object content, ...)` → `WithContent(...)`

```csharp
// Before
manager.Show(myCustomGrid, "MainArea", TimeSpan.MaxValue);

// After
manager.Show(NotificationBuilder
    .Create()
    .WithContent(myCustomGrid)
    .InArea("MainArea")
    .NeverExpires()
    .Build());
```

### WPF-typed values (`Brush`, `ImageSource`)

The Builder uses platform-agnostic types. To pass native WPF objects directly, use the WPF builder
extensions from `Notification.Wpf.Builder`:

```csharp
manager.Show(NotificationBuilder
    .Create("Title", "Message")
    .WithBackground(Brushes.SteelBlue)            // WpfBuilderExtensions
    .WithImage(myImageSource, ImagePosition.Top)   // WpfBuilderExtensions
    .Build());
```

---

## Working examples in the repository

| Topic | File |
|-------|------|
| Builder API & migration examples | [`MainWindow.BuilderExamples.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Wpf.Sample/MainWindow.BuilderExamples.cs) |
| Progress bar & `Report(...)` overloads | [`MainWindow.xaml.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Wpf.Sample/MainWindow.xaml.cs) |
| Cross-platform progress (Avalonia) | [`MainWindow.axaml.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Avalonia.Sample/MainWindow.axaml.cs) |

See also: [Builder API reference](Builder-API.md) · [Getting Started](Getting-Started.md)
