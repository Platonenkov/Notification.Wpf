# Async Show API with Close Reason — Design Spec

- **Date:** 2026-06-08
- **Issue:** [#86 — async overloads? (showasync, ...)?](https://github.com/Platonenkov/Notification.Wpf/issues/86)
- **Target release:** v11.0.0 (major)
- **Affected platforms:** `Notification.Wpf`, `Notification.Avalonia`, `Notification.Maui`, `Notification.Console`
- **Branch:** `claude/async-show-close-reason-9a8660`

## 1. Goal

Add awaitable APIs that return a `Task` completing when a notification closes, with a structured result describing **why** it closed (timeout / body click / X button / action button + button id / cancellation). Provide a parallel API for explicit-choice dialogs with N buttons. Apply consistently across all four supported platforms.

## 2. Non-goals

- Async wrapper for `ShowProgressBar` (separate concern; the progress flow has its own lifecycle and `NotifierProgress`/`Dispose` semantics).
- Full XAML rendering of N-button (>2) notifications in WPF/Avalonia. Until that lands, the third and subsequent buttons are dropped with a `Debug.WriteLine` warning. Planned for a follow-up PR.
- Interactive `Console` stdin reading with cancellation support. Default behavior is non-interactive (fallback always triggers). Opt-in interactive mode is a follow-up.

## 3. Public API surface (Core)

All new types live in `Notification.Core`.

### 3.1 `NotificationCloseReason`

```csharp
public enum NotificationCloseReason
{
    Timeout,        // ExpirationTime elapsed
    BodyClick,      // body click while CloseOnClick = true
    XButton,        // X (close) button pressed
    ActionButton,   // action button pressed; see ButtonId / ButtonIndex
    Cancelled       // CancellationToken cancelled or Dismiss(id) called
}
```

### 3.2 `NotificationCloseResult`

```csharp
public sealed class NotificationCloseResult
{
    public Guid NotificationId { get; init; }
    public NotificationCloseReason Reason { get; init; }
    public string ButtonId { get; init; }     // populated only when Reason == ActionButton
    public int? ButtonIndex { get; init; }    // populated only when Reason == ActionButton
    public bool IsCancelled => Reason == NotificationCloseReason.Cancelled;
}
```

### 3.3 `NotificationButton` and `NotificationButtonRole`

```csharp
public sealed class NotificationButton
{
    public string Id { get; init; }       // required, unique within a request
    public string Content { get; init; }  // user-visible text
    public NotificationButtonRole Role { get; init; } = NotificationButtonRole.Default;
}

public enum NotificationButtonRole
{
    Default,
    Affirmative,
    Negative,
    Destructive
}
```

`Role` is a hint for platform styling and for `NotificationDialogFallback.ShowFirstAffirmative` selection. It does not affect `Reason` (always `ActionButton` regardless of role).

### 3.4 `NotificationDialogFallback`

```csharp
public enum NotificationDialogFallback
{
    ShowFirstAffirmative, // show the first Affirmative button (or the first button if no Affirmative); drop the rest
    SkipSilently,         // do not render the dialog; complete the Task with Reason = Timeout immediately
    Throw                 // throw NotSupportedException synchronously before returning the Task
}
```

### 3.5 Changes to `NotificationRequest`

Add:

```csharp
public IReadOnlyList<NotificationButton> Buttons { get; set; }
```

Mark as obsolete (warning, not error):

```csharp
[Obsolete("Use Buttons collection instead. Will be removed in v12.")]
public Action LeftButtonAction { get; set; }

[Obsolete("Use Buttons collection instead. Will be removed in v12.")]
public string LeftButtonContent { get; set; }

[Obsolete("Use Buttons collection instead. Will be removed in v12.")]
public Action RightButtonAction { get; set; }

[Obsolete("Use Buttons collection instead. Will be removed in v12.")]
public string RightButtonContent { get; set; }
```

Validation rules on `ShowAsync` / `ShowDialogAsync` entry:

- `Buttons` and any of `LeftButton*` / `RightButton*` populated simultaneously → `ArgumentException`.
- Duplicate `Id` within `Buttons` → `ArgumentException`.
- `ShowDialogAsync` called with empty/null `Buttons` → `ArgumentException`.

### 3.6 Changes to `INotificationService`

```csharp
public interface INotificationService
{
    // existing
    Guid Show(NotificationRequest request);
    void Dismiss(Guid notificationId);
    void DismissAll();

    // new
    Task<NotificationCloseResult> ShowAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);

    Task<NotificationCloseResult> ShowDialogAsync(
        NotificationRequest request,
        NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
        CancellationToken cancellationToken = default);
}
```

All four platform implementations are required to provide honest implementations.

### 3.7 Defaults differences between `ShowAsync` and `ShowDialogAsync`

| Aspect | `ShowAsync` | `ShowDialogAsync` |
|---|---|---|
| Default `ExpirationTime` (when null in request) | platform default (5s) | `TimeSpan.MaxValue` |
| Default `CloseOnClick` (when not set in request) | `true` | `false` |
| Buttons required | no (notification with optional action) | yes (≥ 1) |
| `Fallback` behavior | n/a — best-effort | per `NotificationDialogFallback` |

### 3.8 Convenience extension methods (Core)

```csharp
public static class NotificationServiceAsyncExtensions
{
    public static Task<NotificationCloseResult> ShowAsync(
        this INotificationService service,
        string message,
        NotificationType type = NotificationType.None,
        TimeSpan? expirationTime = null,
        string areaName = "",
        CancellationToken cancellationToken = default);

    public static Task<NotificationCloseResult> ShowAsync(
        this INotificationService service,
        string title, string message,
        NotificationType type = NotificationType.None,
        TimeSpan? expirationTime = null,
        string areaName = "",
        CancellationToken cancellationToken = default);
}
```

Both build a `NotificationRequest` and delegate to the core `ShowAsync`.

### 3.9 `NotificationLifecycleEventArgs`

Add nullable field:

```csharp
public NotificationCloseReason? CloseReason { get; }
```

Populated for `Closed` and `Dismissed` stages. Constructor overload accepting reason added; existing constructor preserved.

## 4. Cross-cutting helper (internal)

Internal `NotificationCompletionSource` in `Notification.Core` encapsulates the `TaskCompletionSource<NotificationCloseResult>` plus CT registration plus result assembly. Each platform's manager holds a `ConcurrentDictionary<Guid, NotificationCompletionSource>` and calls `Complete(reason, buttonId, buttonIndex)` at the correct lifecycle point. Not part of the public API.

CT semantics: when the token cancels, the source calls `service.Dismiss(id)` (programmatic close) and completes the Task with `Reason = Cancelled`. Double `Dismiss` is a no-op (idempotent).

## 5. Per-platform implementation

### 5.1 WPF (`Notification.Wpf`)

1. `Controls/Notification.cs`:
   - Replace `public event RoutedEventHandler NotificationClosed` with `public event EventHandler<NotificationCloseEventArgs> NotificationClosed`.
   - Add `NotificationCloseEventArgs` with `Reason`, `ButtonId`, `ButtonIndex`.
   - Internal mutable hint fields (`CloseReason`, `ClickedButtonId`, `ClickedButtonIndex`) set by click/X/timer handlers before the event is raised.

2. `Controls/NotificationArea.cs` (currently around lines 146–186):
   - `OnMouseLeftButtonDown` sets `Reason = BodyClick` (when `CloseOnClick`) or invokes the `onClick` action path.
   - X button (in `Notification.xaml.cs`) sets `Reason = XButton`.
   - Expiration timer in `OnShowContent` sets `Reason = Timeout`.
   - Action button click in `NotificationContent` template sets `Reason = ActionButton` plus the `ButtonId`/`ButtonIndex` from `ResolveButtons`.

3. `Base/NotificationManager.cs`:
   - Existing `Show(NotificationRequest)` unchanged at the call site, but its internal `onClose` wrapper now reads `CloseReason` and completes any matching `NotificationCompletionSource`.
   - New `ShowAsync(NotificationRequest, CancellationToken)` registers a `NotificationCompletionSource`, calls `Show`, returns its `Task`.
   - New `ShowDialogAsync(...)` applies dialog defaults (per §3.7), applies the `Fallback` strategy if the rendered button count is reduced, then delegates to `ShowAsync`.

4. Button rendering (this PR): legacy 2-slot rendering, with `Buttons[0]` → Left, `Buttons[1]` → Right via `ResolveButtons`. Buttons 3+ are dropped with `Debug.WriteLine` warning. Full N-button XAML rendering is a follow-up.

5. `Show(string, string, ..., Action LeftButton, ..., Action RightButton, ...)` positional overload — left as-is. Existing users continue to work.

### 5.2 Avalonia (`Notification.Avalonia`)

1. `AvaloniaNotificationManager.cs`:
   - Same pattern: dictionary of `NotificationCompletionSource`, new `ShowAsync` / `ShowDialogAsync` methods.

2. `AvaloniaNotificationHost` / progress card: lifecycle events extended to carry `NotificationCloseReason`. Verify the host's close paths in implementation — timeout/click/X/Dismiss all route through a single point that emits the reason.

3. Button rendering: same 2-slot strategy as WPF for this PR.

### 5.3 MAUI (`Notification.Maui`)

1. `MauiNotificationManager.cs` (currently lines 52–130):
   - `ShowSnackbar` already handles CT cancellation, action clicks, and timeouts. Extend with explicit reason derivation:
     - `clicked == true` → `Reason = ActionButton`, `ButtonId` = chosen button's `Id`.
     - `ct.IsCancellationRequested` → `Reason = Cancelled`.
     - Otherwise → `Reason = Timeout`.
   - `Toast` path is used only when no buttons are present; `Reason` is always `Timeout` or `Cancelled`.

2. `ShowDialogAsync` and the multi-button case:
   - Snackbar supports exactly one action button.
   - `Buttons.Count == 1` → render as-is.
   - `Buttons.Count > 1` → consult `Fallback`:
     - `ShowFirstAffirmative` (default): pick the first `Affirmative` (or the first overall if none Affirmative); drop the rest with `Debug.WriteLine` warning.
     - `SkipSilently`: complete `Task` with `Reason = Timeout` immediately, do not show.
     - `Throw`: throw `NotSupportedException` synchronously.

### 5.4 Console (`Notification.Console`)

1. `ShowAsync(NotificationRequest, CancellationToken)`:
   - Print the line (existing logic).
   - If `ct.IsCancellationRequested` at entry → return completed Task with `Reason = Cancelled`.
   - Otherwise → return completed Task with `Reason = Timeout` (Console has no "open" state).

2. `ShowDialogAsync(NotificationRequest, NotificationDialogFallback, CancellationToken)` — non-interactive default:
   - `ShowFirstAffirmative` (default): print the variants, return `Reason = ActionButton` with `ButtonId` = first Affirmative (or first overall).
   - `SkipSilently`: print the notification without choices, return `Reason = Timeout`.
   - `Throw`: throw `NotSupportedException` synchronously.

3. Opt-in interactive mode (`ConsoleNotificationManager.InteractiveDialogsEnabled = true`): **out of scope for this PR**. Documented as a follow-up.

## 6. Legacy-to-new conversion helper

Internal helper in `Notification.Core`:

```csharp
internal static class NotificationButtonResolver
{
    public static IReadOnlyList<NotificationButton> ResolveButtons(NotificationRequest request);
}
```

Logic:

1. If `request.Buttons` is non-null and non-empty → return as-is (after duplicate-Id validation).
2. Else, read the obsolete `LeftButtonContent` / `RightButtonContent`. For each non-empty one, build a `NotificationButton` with synthetic `Id` (`"left"` / `"right"`), default role (`Affirmative` / `Negative`).
3. Returns an empty list if none populated.

Click handling in WPF/Avalonia: when the user clicks a button whose source was a legacy field, the corresponding obsolete `LeftButtonAction` / `RightButtonAction` is still invoked, **plus** the `NotificationCompletionSource` completes with the synthetic `ButtonId`.

`#pragma warning disable CS0618` is used internally where legacy fields are read; not exposed to consumers.

## 7. Breaking changes (v11.0.0)

| # | Change | Impact |
|---|---|---|
| 1 | `INotificationService` gains `ShowAsync` and `ShowDialogAsync` | External implementers of the interface must add the new methods. Migration: implement using `NotificationCompletionSource` pattern (documented). |
| 2 | `Notification.NotificationClosed` event signature changes to `EventHandler<NotificationCloseEventArgs>` (WPF) | Subscribers update handler signature; reason and button info now flow via `e.Reason` / `e.ButtonId`. |
| 3 | `LeftButtonAction` / `LeftButtonContent` / `RightButtonAction` / `RightButtonContent` marked `[Obsolete]` | Warning-level; existing code keeps compiling and working. Removal in v12. |
| 4 | `NotificationLifecycleEventArgs` gains nullable `CloseReason` and a new constructor overload | Additive; not breaking. |

Other changes are additive (new types, new fields).

## 8. Documentation updates

The following documents must be updated as part of this PR:

- [README.md](../README.md) — new top-level section "Async API" with the basic `await ShowAsync(...)` example and the `ShowDialogAsync` example.
- [Documentation.md](../Documentation.md) — full reference: `NotificationCloseReason`, `NotificationCloseResult`, `NotificationButton`, `ShowAsync`, `ShowDialogAsync`, extensions, per-platform behavior table.
- [Migration.md](../Migration.md) — new section "Migrating to v11":
  - subscribing to `NotificationClosed` — old vs new signature.
  - migrating `LeftButtonAction`/`Content` to `Buttons[]`.
  - replacing `ShowButtonWindow` callback flow with `await ShowDialogAsync`.
  - external `INotificationService` implementers: how to add `ShowAsync`/`ShowDialogAsync`.
- [Updates.md](../Updates.md) — v11.0.0 release notes with the four breaking changes itemized.
- DocFx XML comments on all new public types and members.

## 9. Sample projects to update

All four samples must demonstrate the new async APIs and be runnable end-to-end:

- [Samples/Notification.Wpf.Sample](../Samples/Notification.Wpf.Sample) — add a tab/page "Async API" with:
  - `await ShowAsync("Saved", NotificationType.Success)`
  - `await ShowDialogAsync(...)` with a 2-button delete confirmation
  - `await ShowDialogAsync(...)` with a 3-button choice and explicit `Fallback`
  - cancellation via a "Cancel" button binding to a `CancellationTokenSource`
- [Samples/Notification.Avalonia.Sample](../Samples/Notification.Avalonia.Sample) — equivalent page.
- [Samples/Notification.Maui.Sample](../Samples/Notification.Maui.Sample) — page demonstrating Snackbar single-action + `ShowFirstAffirmative` fallback for 3-button case.
- [Samples/Notification.Console.Sample](../Samples/Notification.Console.Sample) — CLI flow demonstrating `await ShowAsync(...)` and `ShowDialogAsync(..., ShowFirstAffirmative)`.

## 10. Tests

### 10.1 Core unit tests

1. `NotificationCloseResult` — equality, `IsCancelled`, field assignment.
2. `NotificationButtonResolver` — legacy mapping, empty input, both legacy fields populated, neither populated, `Buttons` priority over legacy.
3. Extension methods build `NotificationRequest` correctly and delegate to `INotificationService.ShowAsync`.
4. Validation — duplicate `Id`, simultaneous `Buttons` + legacy, empty `Buttons` for `ShowDialogAsync` → `ArgumentException`.

### 10.2 Behavioral tests per platform

Each platform's manager covered by:

1. **Timeout** — `ShowAsync` with `ExpirationTime = 100ms` → completes with `Reason = Timeout` within 200ms.
2. **Cancellation** — `ShowAsync` + `CancellationTokenSource.Cancel()` → `Reason = Cancelled`, `Dismiss(id)` invoked.
3. **Dismiss** — `ShowAsync` + concurrent `Dismiss(id)` → `Reason = Cancelled`.
4. **ButtonClick** (WPF/Avalonia) — simulate click on `Id="ok"` → `Reason = ActionButton`, `ButtonId = "ok"`, `ButtonIndex = 0`.
5. **Legacy auto-conversion** — `ShowAsync` with `LeftButtonContent = "OK"`, `LeftButtonAction = action` → click on left invokes `action` AND returns `ButtonId = "left"`.
6. **Idempotency** — double `Dismiss(id)` does not throw; the `Task` is already completed.
7. **DismissAll** — multiple parallel `ShowAsync` calls + `DismissAll()` → all complete with `Reason = Cancelled`.

### 10.3 Platform-specific tests

- **WPF:** `NotificationCloseEventArgs` carries correct `Reason` / `ButtonId`. Progress bar not affected.
- **MAUI:** `ShowDialogAsync` with `Buttons.Count = 3` and each `Fallback` value behaves per spec.
- **Console:** `ShowDialogAsync` non-interactive — each `Fallback` value behaves per spec.
- **Avalonia:** all base reasons.

### 10.4 Out of scope for this PR

- Interactive Console stdin reading.
- Full N-button XAML rendering tests.
- Async progress bar.

## 11. Release checklist (before publishing v11.0.0)

1. All unit and behavioral tests pass on CI (Windows / Linux / macOS as configured).
2. Documentation updated per §8.
3. Sample projects updated per §9.
4. **User explicitly confirms each sample project has been launched and exercises the new async APIs end-to-end** (WPF, Avalonia, MAUI, Console). This is a hard gate before tagging v11.0.0.
5. CHANGELOG / Updates.md entry reviewed.
6. Migration.md migration steps validated against a clean checkout.

## 12. Out of scope (explicit non-goals)

- `Task`-based wrapper for `ShowProgressBar`.
- Full N-button XAML rendering in WPF/Avalonia.
- Interactive Console mode (`InteractiveDialogsEnabled`).
- Cross-platform `ShowFilePopUpMessage` async wrapper (covered by `ShowDialogAsync` indirectly; an explicit overload can be added later if requested).
