### Update list

`v10.0.0`
* **Progress API:** added tuple-free `Report(value)`, `Report(value, message)`, `Report(value, message, title)` and `ReportIndeterminate(...)` overloads
* **Progress API:** added `NotificationProgressReport.Indeterminate(...)` factory and `WithValue`/`WithMessage`/`WithTitle`/`WithShowCancel` helpers
* **Progress API:** added tuple-free `GetSlowedProgress(int)`, `GetValueProgress(...)`, `GetSlowedValueProgress(...)`
* **Obsolete:** tuple-based `GetProgress<T>` / `GetSlowedProgress<T>` marked `[Obsolete]` (still functional)
* **ProgressBar:** added `ShowProgressBar(ProgressBarOptions)` — replaces the 16-parameter positional overload (now `[Obsolete]`)
* **Builder:** added `NotificationBuilder.WithContent(object)` and `NotificationRequest.Content` — custom content via the Builder API
* **Docs:** added [Migration Guide](https://github.com/Platonenkov/Notification.Wpf/blob/dev/Migration.md); full XML documentation across Core, Console, Avalonia, MAUI and WPF
* **Fix #48:** `Dismiss(Guid)` / `DismissAll()` now actually close the target notification (a real dismiss handle is registered per notification)
* **Fix #66:** fixed `InvalidOperationException` race — the overlay window reference is dropped on `Closing` and `Show()` is guarded against a closing window
* **#71:** `NotificationConstants.KeepNotificationVisibleOnMouseOver` — pauses the auto-close timer while the cursor is over the notification
* **#52:** `Notification.CornerRadius` dependency property and `NotificationConstants.NotificationCornerRadius` — rounded notification cards
* **#53:** the close (X) icon now follows the notification `Foreground` and can be styled
* **#65:** `NotificationConstants.OverlayWindowTopmost` — controls whether the overlay window stays on top
* **#54:** right-click support — `NotificationContent.RightClickAction`, `NotificationRequest.OnRightClick`, `NotificationBuilder.OnRightClick`
* All packages aligned to version `10.0.0.0`
* Full backward compatibility — all existing APIs keep working

`v9.0.0`
* **Breaking:** Extracted platform-agnostic `Notification.Core` (netstandard2.0) from monolithic WPF project
* **New platforms:** Added `Notification.Console`, `Notification.Avalonia` (net8.0), `Notification.Maui` (net10.0)
* **Builder API:** Cross-platform fluent builder (`NotificationBuilder`) with priorities, groups, callbacks, text settings, colors
* **Lifecycle Events:** `INotificationEventService` — Shown, Clicked, Closed, TimedOut, Dismissed stages
* **DI support:** `AddNotifications()`, `AddWpfNotifications()`, `AddConsoleNotifications()`, `AddAvaloniaNotifications()`, `UseMauiNotifications()`
* **Queue with priorities:** `INotificationQueue` with Low/Normal/High/Critical priority and group deduplication
* **Cross-platform types:** `NotificationColor`, `NotificationTextSettings`, `NotificationThickness`, `NotificationImageData`
* **Configuration:** `INotificationConfiguration` replaces static `NotificationConstants` (backward compatible — static API still works)
* Added .NET 8/9/10 support for WPF (`net10.0-windows`, `net9.0-windows`, `net8.0-windows`, `net48`)
* Replaced tuple-based progress API with `NotificationProgressReport` struct
* Memory leak fixes in notification lifecycle
* 134 unit tests across Core, Console, and WPF adapter test projects
* Updated CI/CD pipeline for multi-package build, test, and NuGet publish
* Full backward compatibility — all existing WPF APIs (`NotificationManager`, `NotificationArea`, `NotificationConstants`) work unchanged

`v6.1.0.1`
*  corrections in translation

`v6.1.0.0`
*  Added stack control. Use `NotificationConstants.IsReversedPanel` to change stack orientation. Set as `null to default`.
* Message position as `Absolute` with constant setting: `NotificationConstants.AbsolutePosition`

`v6.0.0.0`
* Added net6 supporting

`v5.7.1.2`
* fix text settings for string only messages

`v5.7.1`
* Remove FontAwesome dependency
* Corrects message height calculation when font style changes to non-standart
* Fix SwgAwesome STA error whith progress
  
`v5.7.0`
* Add new sample project in repository
* Add Center screen message position
* Fix progress margin style when no waiting message and button
* Add functions for base message functions (errors view, open file or directory)

  
`v5.6.1`
* Added Image template for Notification content
  
`v5.6.0`
* Added overlay window position from constants,
* ImageSource template for icon,
* X-Close button visible setting
* Add Notification Min and Max Setting

`v5.5.0`
* Added text style settings to content

`v5.4.0`
* Added section for image
* if icon is null - left column will collapse

`v5.3.0`
* access to basic message settings
* setting the number of messages in the window
* settings for the progress bar  to automatically collapse with a large number of messages
* settings progress bar styles
* New progress bar initializing template
* Add more sample in test project

[Constants](https://github.com/Platonenkov/Notification.Wpf/blob/dev/src/Notification.Wpf/Constants/NotificationConstants.cs)

Just change it befor first using

`v5.2.0`
* Color and Icons in settings

`v5.1.0`
* Add Slowed progress bar
* Add Waiting timer for progress bar

`v5.0.0`
* Platform support
