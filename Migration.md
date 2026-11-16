# Migration Guide

Notification.Wpf **10.0** keeps **full backward compatibility** — all existing code keeps compiling and running.
This guide shows how to move to the modern, clearer APIs.
For the breaking changes of **12.0**, see [Upgrading from v11 to v12](#upgrading-from-v11-to-v12).

Two legacy APIs are now marked `[Obsolete]` (they still work, but the compiler will emit a warning).
Migrating away from them removes the warnings and makes call sites far easier to read.

## Upgrading from v11 to v12

v12 is a major release: it **breaks** a small set of WPF APIs and has no compatibility shim. The 10.0 compatibility
promise at the top of this guide applies to 10.0 only. Everything in this section concerns `Notification.Wpf` only;
`Notification.Core`, Avalonia, MAUI and Console behave as in v11.

What changed: each shown notification now has one owner (`NotificationSession`) and one view model
(`NotificationViewModel`). `NotificationContent` is copied into the view model at show time, so a content instance you
pass to `Show` is never mutated and can safely be shown again. The overlay window is closed, not hidden, when its last
notification ends, and it is closed automatically when the last host window closes.

| # | Change | Who is affected | Migration |
|---|--------|-----------------|-----------|
| 1 | `Notification.Content` is a `NotificationViewModel`, not a `NotificationContent` | Code reading `Content` | Use `Source` |
| 2 | Template selectors receive a `NotificationViewModel` | Custom `NotificationTemplateSelector` subclasses that test for `NotificationContent` | Test the view model; read `Source` for the original |
| 3 | The default template key type changes | `DataTemplate DataType="{x:Type wpf:NotificationContent}"` | Change `DataType`; properties of subclasses move to `Source.*` |
| 4 | Templates copied from `Generic.xaml` bind commands | Bindings to `LeftButtonAction` / `RightButtonAction` | Bind `LeftButtonCommand` / `RightButtonCommand` |
| 5 | `AttachOnClick` value type | Templates and code that set or read it, including `GetAttachOnClick` / `SetAttachOnClick` | Bind or pass the view model; the accessors are typed `NotificationViewModel` |
| 6 | The `Notification.IsClosing` setter is no longer public | Code that sets it | Call `Close()` |
| 7 | The `overlayWindow` argument of `Notification.Close` is ignored | Code that passes a window | Stop passing it; the session knows its host |

### 1. `Notification.Content` is a `NotificationViewModel`

```csharp
// Before (v11)
var content = (NotificationContent)notification.Content;
string title = content.Title;

// After (v12)
var viewModel = (NotificationViewModel)notification.Content;   // namespace Notifications.Wpf.ViewModels
string title = viewModel.Title;
NotificationContent original = viewModel.Source;                // the instance you passed to Show
```

`NotificationViewModel` exposes the same names the default template binds (`Title`, `Message`, `Type`, `Image`, `Icon`,
`Background`, `Foreground`, ...), so most reads only need the cast changed. Values are copied when the notification is shown.
Content that is not a `NotificationContent` (a string, an image, a custom object or control) is passed through unchanged.

### 2. Template selectors receive a `NotificationViewModel`

```csharp
// Before (v11)
public override DataTemplate SelectTemplate(object item, DependencyObject container)
{
    if (item is MyContent)
        return (container as FrameworkElement)?.FindResource("MyTemplate") as DataTemplate;
    return base.SelectTemplate(item, container);
}

// After (v12)
public override DataTemplate SelectTemplate(object item, DependencyObject container)
{
    if (item is NotificationViewModel { Source: MyContent })
        return (container as FrameworkElement)?.FindResource("MyTemplate") as DataTemplate;
    return base.SelectTemplate(item, container);
}
```

Selectors that only handle `string` or `ImageSource` items keep working as they are.

### 3. The default template key type changes

```xml
<!-- Before (v11) -->
<!-- xmlns:wpf="clr-namespace:Notification.Wpf;assembly=Notification.Wpf" -->
<DataTemplate DataType="{x:Type wpf:NotificationContent}">
    <StackPanel>
        <TextBlock Text="{Binding Title}"/>
        <TextBlock Text="{Binding Extra}"/>        <!-- property of a NotificationContent subclass -->
    </StackPanel>
</DataTemplate>

<!-- After (v12) -->
<!-- xmlns:vm="clr-namespace:Notifications.Wpf.ViewModels;assembly=Notification.Wpf" -->
<DataTemplate DataType="{x:Type vm:NotificationViewModel}">
    <StackPanel>
        <TextBlock Text="{Binding Title}"/>
        <TextBlock Text="{Binding Source.Extra}"/>
    </StackPanel>
</DataTemplate>
```

### 4. Templates copied from `Generic.xaml`

The default template no longer binds the caller's button actions. Each notification has its own commands, which record the
close reason and button identity, start the close and run your action exactly once. They are `null` when there is no action.

```xml
<!-- Before (v11): the default template went through the library's internal ActionToCommandConverter. -->
<!-- A template outside the library assembly cannot reference it, so Command="{Binding LeftButtonAction}" never worked. -->
<!-- xmlns:wpf="clr-namespace:Notification.Wpf;assembly=Notification.Wpf" -->
<!-- xmlns:controls="clr-namespace:Notification.Wpf.Controls;assembly=Notification.Wpf" -->
<Button Content="{Binding LeftButtonContent}"
        Command="{Binding LeftButtonAction, Converter={wpf:ActionToCommandConverter}}"
        controls:Notification.CloseOnClick="True"/>

<!-- After (v12) -->
<Button Content="{Binding LeftButtonContent}"
        Command="{Binding LeftButtonCommand}"/>
```

`Notification.CloseOnClick` stays public for custom templates; the default template simply no longer needs it on these two buttons.

### 5. `AttachOnClick` value type

```xml
<!-- The markup is unchanged when it binds the template's DataContext -->
<!-- xmlns:controls="clr-namespace:Notification.Wpf.Controls;assembly=Notification.Wpf" -->
<Button controls:Notification.AttachOnClick="{Binding}"/>
```

```csharp
// Before (v11): the attached property was typed as NotificationContent
button.SetValue(Notification.AttachOnClickProperty, content);

// After (v12): it is typed as NotificationViewModel, and so are the accessors
button.SetValue(Notification.AttachOnClickProperty, viewModel);
Notification.SetAttachOnClick(button, viewModel);                       // was SetAttachOnClick(DependencyObject, bool)
NotificationViewModel attached = Notification.GetAttachOnClick(button); // was bool
```

In v11 the `bool`-typed accessors could not work for any input: `GetAttachOnClick` cast the stored value to `bool`, and
`SetAttachOnClick(button, true)` threw inside `SetValue`. Code that called them was already failing, so retyping them
breaks the source of code that did not run.

### 6. `Notification.IsClosing` setter

```csharp
// Before (v11)
notification.IsClosing = true;

// After (v12): IsClosing is read-only; close through the single close path
notification.Close();
```

### 7. `overlayWindow` argument of `Notification.Close`

```csharp
// Before (v11)
notification.Close(overlayWindow);

// After (v12): the argument is still accepted but ignored
notification.Close();
```

### Custom content objects and `INotifyPropertyChanged`

If you show your own data object through a `DataTemplate`, make the object implement `INotifyPropertyChanged` or use
`Mode=OneTime` bindings in the template. WPF itself, not this library, may keep a non-`INotifyPropertyChanged` binding source
alive for a while after the notification has closed: it registers value-changed hooks through `PropertyDescriptor` for each
bound property. The generated visual tree is still collected (the library's tests and soak run show it), so only the data
object itself stays reachable until WPF cleans those hooks up. See `Documentation.md`, section Known Issues.

`NotificationContent` stays a plain class and does not implement `INotifyPropertyChanged`: the view model copies its values
when the notification is shown, so later changes to the instance you passed are not reflected.

### Other behavior changes

- `NotificationRequest.LeftButtonAction`, `LeftButtonContent`, `RightButtonAction` and `RightButtonContent` stay obsolete and are **not** removed in 12.0.
- The overlay window closes when its last notification ends and when the last host window closes, so the `ShutdownMode.OnMainWindowClose`
  workaround is no longer needed.
- A notification shown while its area cannot display completes `ShowAsync` with `Timeout` instead of hanging. It now also raises `NotificationClosed` and runs `onClose`, which v11 skipped.
- The 2-second cap on the close-animation wait is removed. A custom template with a long closing storyboard now delays `NotificationClosed`, `onClose` and the completion of `ShowAsync` by the storyboard's full duration, so keep closing animations short.
- A `Notification` that you construct directly gets the default template only through the two-argument constructor, `new Notification(content, showCloseButton)`. Assigning a `NotificationContent` to `Content` on an instance from the parameterless constructor (or in XAML) does not wrap it into a view model, so the default template does not apply and the object renders through `ToString()`. Notifications created by `NotificationManager` and `NotificationArea` are unaffected.
- Under `ShutdownMode.OnLastWindowClose`, once the last host window has closed, the overlay refuses new toasts until a host window exists again. A refused show is not displayed and raises no `OnClose`, `Shown` or `Closed`; `ShowAsync` completes with `NotificationCloseReason.Cancelled`, and a progress display ends at once as if cancelled.
- A progress bar that cannot be displayed (unknown or unloaded area, overlay failed to open) now ends at once as if cancelled, instead of returning a progress object that nobody sees.

---

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

---

## Migrating from v10.x to v11.0

v11.0 introduces the async API with structured close reasons. Existing code keeps compiling and running —
the changes are additive except for the four items described below.

### 1. WPF `NotificationClosed` subscriber

The `NotificationClosed` routed event now carries `NotificationClosedRoutedEventArgs` instead of the
base `RoutedEventArgs`. The delegate type stays `RoutedEventHandler` and the routing strategy stays
Bubble, so existing handlers typed as `(object s, RoutedEventArgs e)` continue to compile and run
without any changes.

**Before (still compiles in v11):**
```csharp
notification.NotificationClosed += (s, e) =>
{
    // reacted to the fact of close; no reason available
};
```

**After (access the close reason):**
```csharp
notification.NotificationClosed += (s, e) =>
{
    if (e is NotificationClosedRoutedEventArgs rea)
    {
        switch (rea.Reason)
        {
            case NotificationCloseReason.BodyClick:
                HandleBodyClick();
                break;
            case NotificationCloseReason.ActionButton when rea.ButtonId == "ok":
                HandleOk();
                break;
            case NotificationCloseReason.Timeout:
                HandleTimeout();
                break;
        }
    }
};
```

> **Source-compatible:** handlers typed as `(object s, RoutedEventArgs e)` continue to compile because
> `NotificationClosedRoutedEventArgs` derives from `RoutedEventArgs`. Source-breaking only for code that
> previously cast `e` to a different specific args type.

---

### 2. Legacy `LeftButtonAction` / `LeftButtonContent` → `Buttons[]`

The four legacy button properties on `NotificationRequest` are marked `[Obsolete]` in v11. They keep
working (no runtime change), but the compiler will emit CS0618 warnings. Migrate to the `Buttons[]`
collection to remove the warnings and gain access to `ButtonId` in the close result.

**Before:**
```csharp
req.LeftButtonContent  = "OK";   req.LeftButtonAction  = onOk;
req.RightButtonContent = "No";   req.RightButtonAction = onNo;
```

**After:**
```csharp
req.Buttons = new[]
{
    new NotificationButton { Id = "ok", Content = "OK", Role = NotificationButtonRole.Affirmative },
    new NotificationButton { Id = "no", Content = "No", Role = NotificationButtonRole.Negative }
};
```

> The old `LeftButton*` properties continue to work in v11 (translated internally to `Buttons[]`). Removal
> is planned for v12.

---

### 3. `ShowButtonWindow` callback flow → `ShowDialogAsync`

Previously, reacting to button presses required wiring callbacks before calling `Show`. With
`ShowDialogAsync`, the result is returned directly from the awaited call.

**Before:**
```csharp
mgr.ShowButtonWindow("Save changes?", "Title",
    LeftButtonAction:  () => Save(),
    LeftButtonContent: "Save",
    RightButtonAction: () => Discard(),
    RightButtonContent: "Discard");
```

**After:**
```csharp
NotificationCloseResult r = await mgr.ShowDialogAsync(new NotificationRequest
{
    Title   = "Title",
    Message = "Save changes?",
    Buttons = new[]
    {
        new NotificationButton { Id = "save",    Content = "Save",    Role = NotificationButtonRole.Affirmative },
        new NotificationButton { Id = "discard", Content = "Discard", Role = NotificationButtonRole.Negative }
    }
});

if (r.Reason == NotificationCloseReason.ActionButton)
{
    if (r.ButtonId == "save") Save(); else Discard();
}
```

---

### 4. External `INotificationService` implementers

v11 adds two new methods to `INotificationService`. Any class that implements the interface directly
(outside this library) must provide implementations for both.

The simplest no-op implementation returns a completed task with `Reason = Timeout`:

```csharp
public Task<NotificationCloseResult> ShowAsync(
    NotificationRequest request,
    CancellationToken cancellationToken = default)
{
    Guid id = Show(request);   // delegate to existing Show
    return Task.FromResult(new NotificationCloseResult
    {
        NotificationId = id,
        Reason         = NotificationCloseReason.Timeout
    });
}

public Task<NotificationCloseResult> ShowDialogAsync(
    NotificationRequest request,
    NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
    CancellationToken cancellationToken = default)
{
    // Minimal implementation: show and immediately return Timeout
    Guid id = Show(request);
    return Task.FromResult(new NotificationCloseResult
    {
        NotificationId = id,
        Reason         = NotificationCloseReason.Timeout
    });
}
```

For a full implementation with real task completion, model your own `TaskCompletionSource<NotificationCloseResult>`
on the internal `NotificationCompletionSource` pattern used by the built-in platform managers
(see `src/Notification.Core/Internal/NotificationCompletionSource.cs`). Note that
`NotificationCompletionSource` is `internal`; external implementers should create their own
`TaskCompletionSource<NotificationCloseResult>` and wire it to whatever close signal their platform provides.
