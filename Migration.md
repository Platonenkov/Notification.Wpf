# Migration Guide

Notification.Wpf **10.0** keeps **full backward compatibility** — all existing code keeps compiling and running.
This guide shows how to move to the modern, clearer APIs.

Two legacy APIs are now marked `[Obsolete]` (they still work, but the compiler will emit a warning).
Migrating away from them removes the warnings and makes call sites far easier to read.

## Quick reference

| Legacy API | Modern API | Status |
|-----------|-----------|--------|
| `progress.GetProgress<(double,string)>(...)` + tuple `Report((v, msg))` | `progress.Report(value, message)` | ⚠️ `[Obsolete]` |
| `ShowProgressBar(...)` — 16 positional parameters | `ShowProgressBar(ProgressBarOptions)` | ⚠️ `[Obsolete]` |
| `Show(...)` — up to 18 positional parameters | `NotificationBuilder` | ✅ still supported — Builder recommended |

> **Note:** the long-parameter `Show(...)` overloads are **not** deprecated — they keep working without warnings.
> The Builder API is simply the recommended modern alternative.

---

## 1. Progress reporting — drop the tuples

The old API exposed a fake-generic `GetProgress<T>()` that only accepted six hard-coded types
(`double`, `int` and four tuple shapes) and threw `NotSupportedException` at runtime for anything else.

### Before

```csharp
using var progress = manager.ShowProgressBar("Working...");

// fake-generic, tuple-typed, throws at runtime for unsupported T
IProgress<(double, string)> p = progress.GetProgress<(double, string)>(showCancel: true);
p.Report((50.0, "half done"));
```

### After

```csharp
using var progress = manager.ShowProgressBar(new ProgressBarOptions { Title = "Working..." });

progress.Report(50);                              // value only
progress.Report(50, "half done");                 // value + message
progress.Report(50, "half done", "Working");       // value + message + title
progress.Report(50, "half done", "Working", true); // + cancel-button visibility
progress.ReportIndeterminate("Waiting...");        // no numeric value
```

`NotificationProgressReport` also gained immutable helpers:

```csharp
var report = NotificationProgressReport.Indeterminate("Waiting...");
report = report.WithValue(75).WithMessage("Almost there");
```

### Keeping a UI-agnostic calculation layer

If a calculation method must stay free of the notification library, report plain `double` values:

```csharp
// double progress, optional 0..1 -> 0..100 scaling, optional cancel flag
IProgress<double> plain = progress.GetValueProgress(showCancel: true, scale: true);

// throttled passthrough of full reports (one update per 100 ms)
IProgress<NotificationProgressReport> slowed = progress.GetSlowedProgress(updateTimeOut: 100);

// throttled plain double progress
IProgress<double> slowedPlain = progress.GetSlowedValueProgress(updateTimeOut: 100);
```

The obsolete `GetProgress<T>` / `GetSlowedProgress<T>` still work — but prefer the methods above.

📂 **Working example:** [`Samples/Notification.Wpf.Sample/MainWindow.xaml.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Wpf.Sample/MainWindow.xaml.cs) — see the progress demo and the `CalcAsync` methods.

---

## 2. `ShowProgressBar` — 16 parameters → `ProgressBarOptions`

The 16-parameter positional `ShowProgressBar(...)` overload is now `[Obsolete]`.
Replace it with `ShowProgressBar(ProgressBarOptions)`.

### Before

```csharp
using var progress = manager.ShowProgressBar(
    "Processing files",      // Title
    true,                    // ShowCancelButton
    true,                    // ShowProgress
    "MainArea",              // areaName
    false,                   // TrimText
    1,                       // DefaultRowsCount
    "Calculation time",      // BaseWaitingMessage
    true,                    // IsCollapse
    true,                    // TitleWhenCollapsed
    Brushes.DarkSlateGray,   // background
    Brushes.White,           // foreground
    Brushes.LimeGreen,       // progressColor
    null,                    // icon
    null,                    // TitleSettings
    null,                    // MessageSettings
    true);                   // ShowXbtn
```

### After

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

Only set the properties you actually need — every property has a sensible default.

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

**Colors:** `NotificationColor` is a lightweight platform-agnostic struct. Build one from a hex string
(`NotificationColor.FromHex("#2F4F4F")`), RGB (`NotificationColor.FromRgb(47, 79, 79)`), a predefined
color (`NotificationColor.LimeGreen`), or implicitly from a string (`NotificationColor c = "#2F4F4F";`).

> The `ShowProgressBar(ICustomizedNotification content, ...)` overload (custom-content progress) is **not**
> deprecated and keeps working unchanged.

📄 **Source:** [`ProgressBarOptions.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/src/Notification.Core/Models/ProgressBarOptions.cs) · [`IProgressManager.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/src/Notification.Wpf/Base/Interfaces/Base/IProgressManager.cs)
📂 **Working example:** [`MainWindow.BuilderExamples.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Wpf.Sample/MainWindow.BuilderExamples.cs) — `ShowProgressBarOptionsExample`

---

## 3. `Show` — long parameter lists → `NotificationBuilder`

The heaviest `Show(...)` overload takes **18 positional parameters**. The Builder API expresses the
same notification with named, discoverable, chainable methods. The legacy overloads still work — this
migration is recommended, not required.

### Before

```csharp
manager.Show(
    "Update available",                 // title
    "Version 2.0 is ready to install",  // message
    NotificationType.Information,       // type
    "MainArea",                         // areaName
    TimeSpan.FromSeconds(15),           // expirationTime
    () => Log("clicked"),               // onClick
    () => Log("closed"),                // onClose
    () => Install(),                    // LeftButton
    "Install",                          // LeftButtonText
    () => { },                          // RightButton
    "Later",                            // RightButtonText
    NotificationTextTrimType.Trim,      // trim
    3,                                  // RowsCountWhenTrim
    true,                               // CloseOnClick
    null,                               // TitleSettings
    null,                               // MessageSettings
    true,                               // ShowXbtn
    null);                              // icon
```

### After

```csharp
manager.Show(NotificationBuilder
    .Create("Update available", "Version 2.0 is ready to install")
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

The `Show(object content, ...)` overload (arbitrary WPF controls) maps to `WithContent`:

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

The Builder uses platform-agnostic types (`NotificationColor`, `NotificationTextSettings`).
To pass native WPF objects directly, use the WPF builder extensions from `Notification.Wpf.Builder`:

```csharp
manager.Show(NotificationBuilder
    .Create("Title", "Message")
    .WithBackground(Brushes.SteelBlue)                 // WpfBuilderExtensions
    .WithForeground(Brushes.White)                     // WpfBuilderExtensions
    .WithImage(myImageSource, ImagePosition.Top)        // WpfBuilderExtensions
    .Build());
```

📄 **Source:** [`NotificationBuilder.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/src/Notification.Core/Builder/NotificationBuilder.cs) · [`IMessageManager.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/src/Notification.Wpf/Base/Interfaces/Base/IMessageManager.cs)
📂 **Working example:** [`MainWindow.BuilderExamples.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Wpf.Sample/MainWindow.BuilderExamples.cs)

---

## Working examples in the repository

| Topic | File |
|-------|------|
| Builder API & migration examples | [`Samples/Notification.Wpf.Sample/MainWindow.BuilderExamples.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Wpf.Sample/MainWindow.BuilderExamples.cs) |
| Progress bar & `Report(...)` overloads | [`Samples/Notification.Wpf.Sample/MainWindow.xaml.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Wpf.Sample/MainWindow.xaml.cs) |
| Cross-platform progress (Avalonia) | [`Samples/Notification.Avalonia.Sample/MainWindow.axaml.cs`](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Samples/Notification.Avalonia.Sample/MainWindow.axaml.cs) |

See also: [Builder API reference](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Documentation.md) · [Updates / changelog](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Updates.md)
