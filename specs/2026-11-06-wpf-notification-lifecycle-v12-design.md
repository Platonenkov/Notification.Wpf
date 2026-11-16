# WPF Notification Lifecycle Refactor — Design Spec

- **Date:** 2026-11-06
- **Bug report:** [PR #89](https://github.com/Platonenkov/Notification.Wpf/pull/89) by `luoye1314` (heap-dump analysis); README Known Issues
- **Target release:** v12.0.0 (major, breaking changes allowed)
- **Affected platform:** `Notification.Wpf` only
- **Branch:** `claude/wpf-notification-lifecycle-v12-f7f646`, based on `dev` at `b59c74a`
- **Delivery:** one new PR into `dev`, replacing PR #89

## 1. Why

PR #89 reports two real problems, backed by measurements from a host application:

1. **Memory grows on repeated `Show()`.** About 69 MB in 16 minutes; closed notifications stay reachable with their whole visual tree.
2. **The overlay outlives the application.** Under the default `ShutdownMode.OnLastWindowClose` the process keeps running after the main window closes.

Its fix cannot be merged as is. A cold review found three regressions: action buttons lose their `Command`, the hidden overlay can block exit, and a forced overlay close orphans progress polling.

All of these share one cause: **no single object owns a shown notification.** Close logic is spread across `NotificationManager`, `NotificationArea`, `Notification.Close()`, the visual tree (`GetActiveNotificationCount`), a window title lookup and the caller's own `NotificationContent`, which `NotificationArea.Show` mutates. This refactor gives each show one owner and one teardown path.

## 2. What the code and history establish

### 2.1 Overlay window history

| Date | Commit | Change | Reason |
|---|---|---|---|
| 2019-06-04 | `ed36574` | `Hide()` on last notification, re-show on next | Upstream issue #12: empty window in the app switcher |
| 2021-09-01 | `ed5e847`, `a5dd8ba` | `Hide()` replaced by `Close()`; `Show()` removed from the creation block | Upstream issue #10: "If used, process can't be exited" |
| 2026-05-21 | `ffffa86` | `_window` cleared in `Closing`; `Show()` guarded | Issue #66, close race |

The `_window is { IsVisible: false }` branch in `ShowContent` is **not a leftover**: it is the only place that shows a newly created overlay. No recorded reason for window reuse concerns speed or flicker.

### 2.2 Retention chains

| Chain | Reported by | Assessment |
|---|---|---|
| α: `Task.Delay` without cancellation pins the tree | PR #89 | Bounded for expiration: the mouse-over loop exits within 100 ms of closing, the plain delay lasts until expiry. Real and unbounded for progress polling when nothing ends it |
| β: binding to a non-INPC source roots it through `PropertyDescriptor` | PR #89 | Accepted from the measurements; mechanism not yet proven here (see V2) |
| γ: close wait can overflow to `TimeSpan.MaxValue` | PR #89 | **Not reachable as described.** `Math.Min` against the children's maximum removes `MaxValue`. The real failure is an `InvalidOperationException` in `OnApplyTemplate` for a child with `Automatic` duration or an empty storyboard. The default template computes 0.4 s |
| δ: storyboards, bindings and content references are never released | PR #89 | Bindings and content: accepted. Storyboards: unproven, include only if a test requires it (see V4) |
| ε: `NotificationArea.Show` stores a closure capturing the `Notification` in the caller's `LeftButtonAction` / `RightButtonAction` | This spec | The caller's content pins the control. Showing the same instance again nests wrappers, so every earlier notification stays reachable. Also the cause of the shared-content limitation in `specs/roadmap.md` |
| ζ: `NotifierProgress.Area` keeps the `Notification` after it closes | This spec | Lasts while the caller holds the progress object |
| η: static `VisualTreeHelperExtensions._activeControls` keeps the last scanned tree | This spec | Bounded to one scan; removed by counting sessions instead |

### 2.3 Constraints from the current behavior

- `Show(object)` and `NotificationRequest.Content` accept arbitrary content, including a caller-owned `UIElement`. PR #89's recursive walk would clear that element's own bindings, permanently breaking any control the caller shows again.
- Mutating a `NotificationContent` after `Show` does not update the UI today, because plain property setters raise nothing. A snapshot at show time therefore preserves observable behavior.
- `NotificationArea.Unloaded` also fires for temporary unloads, such as switching tabs. It must not close notifications.
- `OnShowContent` returns early when the area is not loaded. The notification is created but never closes, so `ShowAsync` never completes.
- `ShowContent` can run on a manager dispatcher that is not the `Application` thread, where `Application.Windows` throws.

## 3. Goals, non-goals, anti-goals

**Goals**

1. A closed notification becomes collectable, on every close path and for every content kind.
2. The process exits when the last host window closes, whatever the order of window creation.
3. Every shown notification has exactly one teardown, and every awaited `ShowAsync` completes.
4. Action buttons, `CloseOnClick`, close reasons and `ButtonId` / `ButtonIndex` work as in v11.
5. The caller's `NotificationContent` is never mutated.

**Non-goals**

- Full N-button rendering. The roadmap requires a separate UI design review; this design only must not block it.
- Avalonia, MAUI and Console behavior. Their code is untouched.
- Replacing the overlay `Window` with a non-window host. An `HwndSource` overlay would never count as an application window, but it rewrites transparency, DPI and topmost behavior, which cannot be judged without a spike.
- Fixing exceptions thrown by caller callbacks. Behavior stays as in v11.

**Anti-goals: failure even if the leak numbers improve**

- A click on an action button that does not run the action, or that reports `Timeout`.
- Any change to a caller-owned control or its bindings.
- An overlay that closes while another host window is still open.
- A fix with no test that fails on `dev` and passes after it.

## 4. Design

### 4.1 `NotificationViewModel`: one per show

`NotificationArea` builds a new `NotificationViewModel : ViewModel` for each show of a `NotificationContent`, including subclasses. It becomes `Notification.Content`.

- Public type, internal constructor, so consumers can use it in `DataType` and selectors.
- Exposes the same names the default template binds today: `Title`, `Message`, `Type`, `Image`, `Icon`, `IconForeground`, `Background`, `Foreground`, text settings, `TrimType`, `RowsCount`, button content. Existing `{Binding Title}` templates need only the `DataType` change.
- Values are copied at show time.
- Exposes `Source`, the original content, so templates for `NotificationContent` subclasses can reach their own properties with `{Binding Source.Extra}`. A reference from the view model to the content is harmless: the view model is dropped at close.
- Exposes `LeftButtonCommand` and `RightButtonCommand`. Executing one records the reason and button identity on the session, starts the close and runs the caller's action exactly once. The default template uses these commands and no longer needs `CloseOnClick` on those two buttons.

Consequences: chain ε disappears, the shared-content limitation in the roadmap disappears, and two areas with the same name each get their own view model. `AttachOnClick` and `TextContentView` must accept the view model.

The `CloseOnClick` attached property stays public for custom templates. The yield before reading the close reason stays too, because such templates still depend on `Command` running after `Click`.

### 4.2 `NotificationSession`: the owner

An internal class, one per displayed notification. It owns the `Notification` control, the view model, one `CancellationTokenSource`, the pending reason and button identity, and the callbacks. `NotificationArea` keeps its sessions in a list.

- Timers and progress polling observe the session token.
- Eviction, expiry, `Dismiss`, the X button, body click, action buttons, `NotifierProgress.Dispose` and window teardown all go through one idempotent close operation.
- Public `Notification.Close(...)` remains and delegates to it. The `overlayWindow` parameter stays in the signature but is ignored.
- A `Notification` created outside `NotificationArea` lazily creates a session with no area, so there is one close path and no legacy branch. Steps that need an area are skipped.

**When the area cannot display.** This means the area is not loaded, its `PresentationSource` is null, or `_items` is null. The session still exists:

1. `onCreated` runs first, so `ShowAsync` has subscribed to `NotificationClosed`.
2. The session then closes through the normal path with reason `Timeout`, matching the unknown-`AreaName` result, and skips the animation wait.
3. `onClose`, the `Closed` lifecycle event and the `_dismissActions` / `_completions` cleanup all run as for any close.

### 4.3 Teardown order

| Step | Action | Why here |
|---|---|---|
| 1 | Mark closing; cancel the session token | Stops timers and polling |
| 2 | Raise `NotificationCloseInvoked` | Starts the animation; bindings stay live |
| 3 | Yield at least once, then wait the measured animation time | A custom template's `Command` must still run. No 2-second cap: the wait is bounded by the template |
| 4 | Read the close reason | Everything that sets it has run |
| 5 | Release extra resources only if a collectability test requires it (see below) | Nothing is released before every handler of the click has run |
| 6 | Raise `NotificationClosed` | Unchanged public contract; `ShowAsync` completes |
| 7 | Remove from the area panel and session list; clear `NotifierProgress.Area`; dispose the token source | Drops the library's own references |
| 8 | If the overlay has no sessions left, close it | See 4.4 |

**No binding walk.** `BindingOperations.ClearAllBindings` clears only local values. Bindings that a template creates are template values and survive it. `luoye1314` reported this from a control experiment on PR #89 (`BaseValueSource = ParentTemplate`), and V7 proves it here. Walking the generated tree would therefore clear nothing that matters. It would only damage local bindings inside caller-owned elements.

Release therefore means:

- **Drop references in step 7.** The generated tree becomes unreachable from the session, the area and the window.
- **Do not null `Content`.** Measured in Task 3: setting `Content` and `ContentTemplate` to `null` changes nothing. Without an outside reference the control is collected anyway; with one, the detached `ContentPresenter` keeps its generated tree because no layout pass runs again. Holding a closed control is caller misuse; the library's own holder, `NotifierProgress.Area`, is cleared in step 7.
- **Create no reference from content back to the control.** Chain ε is exactly such a reference, and 4.1 removes it. Without a path from the source to the target, the static `PropertyDescriptor` table cannot keep the notification alive.
- **Never modify a caller-owned `UIElement`.**

For `NotificationProgress`, the library owns the view but the caller may hold the view model, so its model is released through steps 1 and 7. If a collectability test still fails after this, find the retention path before adding any release mechanism.

Closing-time calculation keeps PR #89's approach: only durations with `HasTimeSpan` count.

### 4.4 Overlay lifetime

1. **The overlay closes when its last session ends** (`Close()`, as in v11). `Hide()` is not reintroduced. The active count comes from sessions, replacing the visual tree scan and the `"ToastWindow"` title lookup.
2. **A host window watcher** replaces any dependency on `Application.MainWindow`:
   - On each overlay show, subscribe to `Closed` on every window in `Application.Windows` except the overlay that is not yet tracked.
   - When a tracked window closes, stop tracking it and rescan. This keeps one invariant: while any host window exists, at least one is tracked, so windows opened later are never missed.
   - If `ShutdownMode` is `OnLastWindowClose` and no host window remains, close all overlay sessions with reason `Cancelled`. The overlay then closes through step 8, and WPF ends the process.
   - `OnMainWindowClose` needs nothing: WPF shuts down itself. `OnExplicitShutdown` is left to the host.
   - Skipped when `Application.Current` is null or its dispatcher is not the current one.
3. **The overlay should not stay `Application.MainWindow`.** WPF assigns the first window created, so an overlay created at startup would take that role. If it did, the overlay closing under `OnMainWindowClose` would shut the application down. Right after creating the overlay, if `Application.Current?.MainWindow` is the overlay, set it to `null`. Never touch a value the host assigned. **V3 is not a gate:** the watcher does not use `MainWindow`, so goal 2 holds either way. If the reset proves impossible, this hazard stays as v11 behavior and goes into `specs/roadmap.md`.
4. **A window hosting a named `NotificationArea` ends its sessions on `Window.Closed`**, not on `Unloaded`. This closes the unbounded progress loop on that path too.

If a host window closes while another stays open, the overlay stays open. That removes the trigger of PR #89's third regression.

**Known and unchanged:** if an overlay toast expires before any host window exists, the overlay is the last window and its close may shut the application down under `OnLastWindowClose`. This is v11 behavior. V6 confirms it; it then goes into `specs/roadmap.md` unless the fix proves trivial.

### 4.5 Progress notifications

`NotificationProgressViewModel` stays as it is; it is already a real view model. Its display gets a session like any other, and polling uses the session token linked with `progress.Cancel`. A progress notification closed from any path therefore ends its loop and runs `CancelProgress` as today.

## 5. Breaking changes

To be listed in `Migration.md` and `Updates.md`:

| Change | Who is affected | Migration |
|---|---|---|
| `Notification.Content` is `NotificationViewModel`, not `NotificationContent` | Code reading `Content` | Use `Source` |
| Selectors receive `NotificationViewModel` | Custom `NotificationTemplateSelector` subclasses testing `NotificationContent` | Test the view model; read `Source` for the original |
| Default template key type changes | `DataTemplate DataType="{x:Type wpf:NotificationContent}"` | Change `DataType`; subclass properties move to `Source.*` |
| Templates copied from `Generic.xaml` | Bindings to `LeftButtonAction` / `RightButtonAction` | Bind `LeftButtonCommand` / `RightButtonCommand` |
| `AttachOnClick` value type | Templates setting it | Bind to the view model |
| `Notification.IsClosing` setter is no longer public | Code setting it | Call `Close()` |
| `overlayWindow` argument of `Notification.Close` is ignored | Code passing a window | Stop passing it; the session knows its host |

`NotificationContent` itself stays a plain data class, with no `INotifyPropertyChanged`. All five packages move to `12.0.0.0`, keeping the versions aligned.

## 6. Verification

### 6.1 Claims to prove first

Each of these comes from memory or from the bug report. Write the test before depending on it.

| Id | Claim | How |
|---|---|---|
| V1 | `ButtonBase.OnClick` raises `Click` before executing `Command` | STA test with a real `Button` |
| V2 | A plain-object binding source stays reachable; an INPC source does not | `WeakReference` test |
| V3 | `Application.MainWindow` can be reset once the overlay has taken it | Test in a separate process |
| V4 | Template storyboards do or do not retain the control | `WeakReference` test without storyboard handling; add handling only if it fails |
| V5 | After step 7 the generated tree is unreachable | `WeakReference` test on an element of the generated tree |
| V6 | Closing the only window before any host window exists shuts the application down | Test in a separate process |
| V7 | `ClearAllBindings` leaves template-applied bindings in place | Test on a template-generated element |

### 6.2 Automated tests in `Tests/Notification.Wpf.Tests`

Use the existing `StaTask` helper. These tests need a real, loaded `NotificationArea`, unlike the headless tests there today:

- Host the area in a real window placed off-screen, with `ShowActivated=false` and `ShowInTaskbar=false`, so it does not take focus.
- Mark them with a `Gui` trait and put them in one non-parallel xUnit collection, because `Areas` and `_window` are static. Add an internal reset hook for those statics.
- Run them in the `MauiLab` VM during development and in CI on `windows-latest`. If CI cannot show windows, exclude the `Gui` trait there and keep the VM run as the gate.

Each must fail on `dev` or on PR #89 where a defect exists there:

- Action button: command runs once, `ShowAsync` reports `ActionButton` with the right `ButtonId` / `ButtonIndex`.
- Collectability after close, for each path (expiry, X button, body click, button, `Dismiss`, eviction, window close) and each content kind.
- Showing one `NotificationContent` twice: the caller's content is unchanged, and the first notification is collected.
- A caller-owned `UIElement` keeps its bindings after close and can be shown again.
- Progress closed through window teardown: the loop ends and `CancelProgress` runs.
- `ShowAsync` completes when the area cannot display.

### 6.3 Runs in the `MauiLab` VM

GUI runs happen in the VM, not on the host desktop. The VM exists and is a merge gate.

**Exit matrix, in a small test application.** Dimensions: overlay created before or after the main window; toast idle, active, or progress running; second window open or closed; each `ShutdownMode`. The builder may pick a representative subset, but it must cover every value of every dimension, plus the PR #89 regression cases.

| `ShutdownMode` | Pass |
|---|---|
| `OnLastWindowClose` | Process exits within 5 seconds of the last host window closing, and stays alive while any host window is open |
| `OnMainWindowClose` | Process exits within 5 seconds of the main window closing |
| `OnExplicitShutdown` | Process stays alive until the host calls `Shutdown`, then exits within 5 seconds |

In every cell, pending `ShowAsync` tasks complete before exit whenever the overlay was closed by the watcher.

**Soak.** PR #89 does not publish its load, so this spec defines one: about 5 notifications per second for 1 hour, mixing plain text, action buttons, a custom data object with a `DataTemplate`, repeated shows of one `NotificationContent`, and periodic progress notifications.

Take dumps after 10 minutes and at the end, each after a forced GC with no notification visible. Pass: no closed `Notification` instance remains, and managed heap at the end is within 10% of the 10-minute dump. PR #89's own result (581 to 7 instances over 12 hours) is a reference, not the criterion.

### 6.4 Before merge

Cold review of the final diff, per the `cold-diff-review` skill. Run hunk reversion on every finding.

## 7. Decisions

**Made by the repository owner:** a separate PR replaces PR #89; a view model layer rather than `INotifyPropertyChanged` on `NotificationContent`; release as v12 with breaking changes allowed.

**Calls made in this design, open to objection:**

1. `Close()` rather than `Hide()`: no recorded value for reuse, and `Hide()` is what brought issue #10 back.
2. Window watcher rather than `Owner`: `Owner` would minimize toasts with the host window and needs the window to exist.
3. N-button rendering stays out.
4. Default template moves to commands; `CloseOnClick` stays for custom templates.
5. All five packages bump to 12.0.0.0.
6. The 2-second close cap is dropped.
7. The startup edge case in 4.4 stays unfixed unless trivial.
8. `Notification.IsClosing` loses its public setter; the `overlayWindow` argument is ignored.
9. A `Notification` created outside an area gets a session with no area, rather than a legacy path.
10. "Cannot display" closes through the normal path with `Timeout`, after `onCreated`.
11. V3 does not block the PR.
12. Lifecycle tests use off-screen windows in a non-parallel `Gui` collection.
13. The soak profile and thresholds in 6.3 are defined here, because PR #89 does not publish its load.
14. No binding walk at close (V7); release by dropping references and removing chain ε. Changed on 2026-11-15 after the PR #89 author's reply.
15. `Content` is not set to `null` at close, so it is not a breaking change. A mutation run and a retained-control test showed the nulling has no effect on collectability (2026-11-15).

**Left to the builder:** class locations, the exact namespace of the view model, how the session list is exposed to the manager, the shape of the exit test application, commit order.

## 8. Credit

The memory report, its dumps and the four retention chains come from `luoye1314`. Reference PR #89 in the release notes, and close it with an explanation when the replacement merges.
