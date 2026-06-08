# Roadmap — Post v11.0.0

This file captures follow-up items deferred from the v11 async API release. Each item links to its prompting spec and explains the chosen direction.

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
