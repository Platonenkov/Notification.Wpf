# Roadmap — Post v11.0.0

This file captures follow-up items deferred from the v11 async API release. Each item links to its prompting spec and explains the chosen direction.

## Known limitations of the v11 async API

Surfaced during the final code review; accepted for v11 and tracked here rather than fixed, because each is an edge case, a platform-API gap, or a pre-existing behavior.

### Platform-API gaps (MAUI)
- **`XButton` is unreachable on MAUI.** CommunityToolkit Snackbar/Toast expose no "user dismissed" callback, so closing a toast via the platform X resolves as `Timeout`, not `XButton`. Reachable MAUI reasons: `ActionButton`, `Timeout`, `Cancelled`.
- **`Dismiss(id)` / cancellation cannot visually close a shown action sheet.** `DisplayActionSheetAsync` has no programmatic-dismiss API. The awaited Task correctly resolves as `Cancelled`, but the native sheet stays on screen until the user interacts; a late tap is ignored. The Toast path does dismiss correctly.
- **Two dialog buttons with identical `Content` are ambiguous on MAUI.** The action-sheet result is matched back to a button by its display text, so identical `Content` returns the first match's `ButtonId`. Give dialog buttons distinct `Content` on MAUI. (Button `Id` uniqueness is validated; `Content` uniqueness is not.)
- **No-button Toast visual duration ignores `ExpirationTime`.** The Toast shows for its native (~2s) duration while the awaited Task honors the requested expiration; the two can differ. MAUI's Toast API does not expose an arbitrary duration.
- **`OnClose`/`Closed` ordering on external-CT cancellation.** When the caller's `CancellationToken` fires, the awaited Task resolves immediately; the `OnClose` callback and `Closed` lifecycle event fire on a subsequent continuation, i.e. slightly after `await` resumes.
- **Legacy fire-and-forget `Show(NotificationRequest)` on MAUI is `async void` and only catches `OperationCanceledException`.** Pre-existing; the async API path catches all exceptions.

### Edge cases (WPF / Avalonia)
- **Avalonia: first async show in overlay-window mode can self-complete as `Timeout`.** The overlay window is created asynchronously; a `ShowAsync` issued before it exists takes the no-host branch and resolves `Timeout` without displaying. Initialize the overlay window before the first async show.
- **WPF/Avalonia: `ShowDialogAsync` and `ProjectButtonsOntoLegacySlots` mutate the caller's `NotificationRequest`** (button slots, `ExpirationTime`, `CloseOnClick`). Treat a request instance as single-use; do not inspect/reuse it after showing.
- **WPF: a `NotificationManager` bound to a second dispatcher still shares thread-affine state with the application thread.** The static `NotificationConstants` brushes are unfrozen and belong to the thread that first touched them, and application-wide theme resources are shared, so rendering a toast on another thread can fail with a cross-thread `DependencySource` error (observed in the exit host, case `toast-thread`). v12 keeps the host-window watcher and its latch off such an overlay, so the application still exits cleanly, but the configuration itself is fragile until those brushes are frozen or created per thread.
- **WPF: a toast that expires before any host window exists can shut the application down.** With `ShutdownMode.OnLastWindowClose`, the overlay is then the only window, and closing it ends the process. Observed in the exit matrix (claim V6): the application shut down after the overlay's toast expired. This is v11 behavior and is unchanged. Open the host window before the first toast, or use `OnExplicitShutdown`.

### Fixed in the review pass (for reference)
Avalonia `CloseAll`/`DismissAll` hanging awaited tasks, duplicate in-flight `Id` stranding tasks, WPF unknown-`AreaName` hang, WPF zero-length-animation wrong reason, and Console missing validation were all fixed before tagging v11.

## RunWithProgressAsync

Source: [specs/2026-06-08-async-show-with-close-reason-design.md §12.1](2026-06-08-async-show-with-close-reason-design.md).

**Locked shape: thin Run delegate.**

```csharp
Task RunWithProgressAsync(
    ProgressBarOptions options,
    Func<IProgress<NotificationProgressReport>, CancellationToken, Task> work,
    CancellationToken cancellationToken = default);

Task<TResult> RunWithProgressAsync<TResult>(
    ProgressBarOptions options,
    Func<IProgress<NotificationProgressReport>, CancellationToken, Task<TResult>> work,
    CancellationToken cancellationToken = default);
```

- Cancellation: standard `OperationCanceledException` (no structural `IsCancelled` field).
- WPF / Avalonia / Console: native progress UI on top of the existing `ShowProgressBar` infrastructure.
- MAUI: indeterminate Snackbar showing the latest `Message`; numeric `Value` updates accepted but not rendered.
- Lifecycle: progress UI shown before `work` runs, closed when `work` completes (including on exception).
- Precondition: extract `ProgressBarOptions` from WPF to Core (or introduce a Core counterpart) so the API is portable.
- Does **not** replace `ShowProgressBar` — both coexist; handle-based form stays for cases that need to share the handle across components.

Target release: v11.1 (minor) or v12.

## Full N-button XAML rendering (WPF and Avalonia)

Source: spec §5.1.4, §5.2.3.

Replace the legacy 2-slot button rendering in `NotificationContent` with an `ItemsControl` driven by `NotificationRequest.Buttons`. Update styles, default-button focus, and visual regressions for light/dark themes.

Target release: v11.1 or later, separate UI design review required.

## Interactive Console dialogs

Source: spec §5.4, §12.

Opt-in `ConsoleNotificationManager.InteractiveDialogsEnabled = true` reads stdin to resolve `ShowDialogAsync`. Required work:
- Reliable async stdin read with `CancellationToken` support across .NET 6 / 8 / 10 (Polyfill for `Console.In.ReadLineAsync(CancellationToken)` on older TFMs).
- `Console.IsInputRedirected` detection to fall back to non-interactive mode in CI.
- Number parsing and validation, Esc / empty input handling.
- Progress-bar interaction safety.

Target release: on demand only.

## Cross-platform ShowFilePopUpMessageAsync

Source: spec §12.

Currently expressible as a one-liner over `ShowDialogAsync`. Add a convenience overload only if usage shows broad demand.

Target release: opportunistic.

## ShowProgressBar to an area that never displays

`ShowProgressBar` to an unknown or unloaded area, or after a failed overlay `Show()`, returns a `NotifierProgress` that was never displayed. Pre-existing. Needs a NotDisplayed outcome so callers can tell.

Target release: on demand only.
