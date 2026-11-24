# WPF Notification Lifecycle v12 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every shown WPF notification one owner and one teardown path, so closed notifications are collectable, action buttons keep working, and the process exits with its last host window.

**Architecture:** `NotificationSession` owns each displayed notification and runs the only close path. `NotificationViewModel` is a per-show snapshot that replaces mutation of the caller's `NotificationContent`. `HostWindowWatcher` closes the overlay when the last host window closes, without using `Application.MainWindow`.

**Tech Stack:** C#, WPF, targets `net10.0-windows;net9.0-windows;net8.0-windows;net4.8-windows`; xUnit v3 with built-in `Assert`.

**Spec:** `specs/2026-11-06-wpf-notification-lifecycle-v12-design.md`. Read it first: section 2 explains why each task exists.

## Global Constraints

- Branch `claude/wpf-notification-lifecycle-v12-f7f646`. Do not base any work on `pr-89`, and do not copy code from it without a test that requires it.
- Only `src/Notification.Wpf`, its tests, samples and docs change, plus the `<Version>` line of the other four packages.
- `NotificationContent` and `BaseNotificationContent` get no `INotifyPropertyChanged`.
- `Hide()` is not used on the overlay. No 2-second close cap. No `Owner` on the overlay.
- Style: explicit types instead of `var`, `_camelCase` private fields, English comments that explain why, XML docs on public members. Keep every file's encoding and BOM. Code must compile for `net4.8-windows`.
- Tests: xUnit v3 built-in `Assert` only. No FluentAssertions.
- Commits: conventional format, no `Co-Authored-By` or other AI attribution.
- GUI rule: any run that opens real windows goes through the `maui-vm-lab` skill, in VM `MauiLab`. On the host run only `dotnet test Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj -c Release --filter "Category!=Gui"`. Each `Gui` run below means the same project and configuration with `--filter "Category=Gui"`, in the VM.
- Run tests in `Release`, as CI does: collectability assertions are unreliable in `Debug`.

## Review Focus

Each line has a test in the owning task.

1. A caller-owned `UIElement` nested in the generated tree, as `Icon` or button content, must keep its bindings after close (Task 3).
2. One `NotificationContent` shown at once in two areas with the same name: each button reports only for its own notification (Task 4).
3. A double click on an action button runs the action once; an action that throws still lets the notification close (Task 4).
4. `Close()` or `Dismiss` called from inside a `NotificationClosed` handler, or during the animation, must not repeat `onClose` (Task 2).
5. A toast shown while the last overlay session is finishing must not be lost when the overlay closes (Task 6).

---

### Task 1: Test harness and proof of the WPF claims

**Files:**
- Modify: `Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj`
- Modify: `src/Notification.Wpf/Notification.Wpf.csproj`
- Modify: `src/Notification.Wpf/Base/NotificationManager.cs`
- Create: `Tests/Notification.Wpf.Tests/Lifecycle/GuiCollection.cs`, `GuiHost.cs`, `GcAssert.cs`, `WpfClaimsTests.cs`

**Interfaces:**
- Produces:
  - `[CollectionDefinition("Gui", DisableParallelization = true)] public class GuiCollection`. Every GUI test class carries `[Collection("Gui")]` and `[Trait("Category", "Gui")]`.
  - `internal sealed class GuiHost : IDisposable` with `static Task<GuiHost> CreateAsync(string areaName = "gui")`, `Window Window`, `NotificationArea Area`, `NotificationManager Manager`. The window is off-screen (`Left = -10000`), `ShowActivated = false`, `ShowInTaskbar = false`. `CreateAsync` returns after `Area.IsLoaded`. `Dispose` closes the window and calls the reset hook.
  - `internal static class GcAssert` with `Task CollectedAsync(WeakReference reference)` and `Task NotCollectedAsync(WeakReference reference)`. Three rounds of `await Dispatcher.Yield(DispatcherPriority.ApplicationIdle)`, `GC.Collect()`, `GC.WaitForPendingFinalizers()`, `GC.Collect()`. Tests create the weak reference in a separate `[MethodImpl(MethodImplOptions.NoInlining)]` method.
  - `internal static void NotificationManager.ResetStateForTests()`: closes `_window`, clears `Areas`.

- [ ] **Step 1: Make internals visible to the tests.** Sign the test project the way `Tests/Notification.Core.Tests/Notification.Core.Tests.csproj` does (`SignAssembly`, `AssemblyOriginatorKeyFile` to `..\..\src\Notification.Wpf\Notifications.Wpf.snk`). In `Notification.Wpf.csproj` add `<InternalsVisibleTo Include="Notification.Wpf.Tests, PublicKey=..."/>` with the key string from `src/Notification.Core/Notification.Core.csproj:46`.

- [ ] **Step 2: Add the helpers and `ResetStateForTests` listed above.**

- [ ] **Step 3: Write the claim tests.** Headless, no `Gui` trait, run through `StaTask`.

```csharp
[Fact] public void V1_ButtonClick_RaisesClickBeforeCommand()
{
    // Button subclass exposing OnClick; Click and Command each append to a list.
    Assert.Equal(new[] { "click", "command" }, order);
}

[Fact] public void V2_PlainObjectBindingSource_IsNotCollected() =>
    StaTask.Run(async () => await GcAssert.NotCollectedAsync(BindPlainSource()));

[Fact] public void V2_InpcBindingSource_IsCollected() =>
    StaTask.Run(async () => await GcAssert.CollectedAsync(BindInpcSource()));
```

`BindPlainSource` and `BindInpcSource` bind a `TextBlock.Text` in `OneWay` mode to a source object, then drop both.

```csharp
[Fact] public void V7_ClearAllBindings_LeavesTemplateBindingInPlace()
{
    // ContentControl with a DataTemplate whose TextBlock binds Text; apply the template;
    // BindingOperations.ClearAllBindings(textBlock);
    Assert.NotNull(BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty));
}
```

- [ ] **Step 4: Run them.** `dotnet test Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj -c Release --filter "FullyQualifiedName~WpfClaimsTests"`. Expected: PASS. These prove WPF behavior, so they pass on `dev`. **If V1, V2 or V7 fails, stop and report it:** sections 4.1 and 4.3 of the spec rest on them.

- [ ] **Step 5: Confirm the harness works.** Add `GuiHost_LoadsArea` with the `Gui` trait, asserting `host.Area.IsLoaded`. Run it in the VM. Expected: PASS.

- [ ] **Step 6: Commit** `test(wpf): add lifecycle test harness and prove WPF binding claims`.

---

### Task 2: `NotificationSession` and the single close path

**Files:**
- Create: `src/Notification.Wpf/Controls/Notifications/NotificationSession.cs`
- Modify: `src/Notification.Wpf/Controls/Notifications/PART_CloseButton.cs`, `Notification.cs`, `src/Notification.Wpf/Controls/NotificationArea.cs`, `src/Notification.Wpf/Base/NotificationManager.cs`
- Test: `Tests/Notification.Wpf.Tests/Lifecycle/NotificationSessionTests.cs`

**Interfaces:**
- Produces (namespace `Notification.Wpf.Controls`):

```csharp
internal sealed class NotificationSession
{
    internal NotificationSession(Notification control);
    internal Notification Control { get; }
    internal NotificationArea Area { get; set; }            // null outside an area
    internal CancellationToken Token { get; }
    internal bool IsClosing { get; }
    internal NotificationCloseReason? PendingReason { get; set; }
    internal string PendingButtonId { get; set; }
    internal int? PendingButtonIndex { get; set; }
    internal void RequestClose(NotificationCloseReason? reason = null); // PendingReason ??= reason, then close
    internal Task CloseAsync(bool skipAnimation = false);               // idempotent; returns the one close task
}
// Notification: internal NotificationSession Session { get; }   lazily created
// Notification.IsClosing: public, get-only, returns Session.IsClosing
// NotificationArea:
internal IReadOnlyList<NotificationSession> ActiveSessions { get; }  // UI thread only; not closing, not yet removed
internal void CancelAll(NotificationCloseReason reason);
```

- `Notification.PendingCloseReason`, `PendingButtonId`, `PendingButtonIndex` are removed; callers use the session.

- [ ] **Step 1: Write the failing tests.** `Gui` trait unless marked.

```csharp
[Fact] Close_Twice_RaisesClosedAndOnCloseOnce()          // Assert.Equal(1, closedCount); Assert.Equal(1, onCloseCount)
[Fact] Close_CancelsSessionToken_BeforeCloseInvokedHandlers() // Assert.True(tokenCancelledInHandler)
[Fact] PendingReason_SetAfterCloseStarts_IsReported()    // RequestClose(); then set ActionButton synchronously; Assert.Equal(ActionButton, result.Reason)
[Fact] Expiry_ReportsTimeout()                           // ShowAsync with 50 ms expiry
[Fact] XButton_ReportsXButton()
[Fact] Dismiss_ReportsCancelled()
[Fact] Eviction_ReportsCancelled()                       // MaxItems = 1, two shows; first result Cancelled
[Fact] CloseFromClosedHandler_DoesNotRepeatOnClose()     // Review Focus 4
[Fact] DismissDuringAnimation_DoesNotRepeatOnClose()     // Review Focus 4
[Fact] CannotDisplay_RunsOnCreatedThenClosesWithTimeout()
    // headless: new NotificationArea().Show(...) on an unloaded area;
    // onCreated subscribes NotificationClosed; Assert order "created", "closed"; reason Timeout; onClose ran once
[Fact] NotificationWithoutArea_ClosesOnce()              // headless: new Notification("x", true).Close()
```

- [ ] **Step 2: Run them.** Expected: compile failure, because `NotificationSession` does not exist yet.

- [ ] **Step 3: Implement `NotificationSession` and move the close body into `CloseAsync`.** Follow the order in spec 4.3, steps 1 to 8. In this task step 5 is empty (Task 3 fills it) and step 8 keeps the existing overlay logic (Task 6 replaces it). The yield stays as it is: `Task.Delay` for a positive animation time, `Dispatcher.Yield(DispatcherPriority.Background)` otherwise, and only the dispatcher yield when `skipAnimation` is true. Dispose the session's `CancellationTokenSource` in step 7.

- [ ] **Step 4: Route every close through the session.**
  - `Notification.Close(Window overlayWindow = null)` stays `public async void`, ignores the argument, awaits `Session.CloseAsync()`.
  - In `NotificationArea` and `NotificationManager`, replace each `n.PendingCloseReason ??= x; n.Close(...)` with `n.Session.RequestClose(x)`.
  - `OnShowContent` sets `session.Area`, adds to the session list, and uses `session.Token` for both delay forms. Catch `OperationCanceledException` and return.
  - When `!IsLoaded`, the `PresentationSource` is null or `_items` is null: after `onCreated` has run, set `session.PendingReason ??= NotificationCloseReason.Timeout` and call `session.CloseAsync(skipAnimation: true)`.

- [ ] **Step 5: Run the tests.** Non-GUI on the host, `Gui` in the VM. Expected: PASS, including all existing tests.

- [ ] **Step 6: Commit** `refactor(wpf): route every notification close through NotificationSession`.

---

### Task 3: Release content at the right moment

**Files:**
- Modify: `src/Notification.Wpf/Controls/Notifications/Notification.cs`, `NotificationSession.cs`
- Test: `Tests/Notification.Wpf.Tests/Lifecycle/ContentReleaseTests.cs`

**Interfaces:**
- Consumes: `NotificationSession` step 7.
- Produces: `internal static TimeSpan Notification.ComputeClosingAnimationTime(ControlTemplate template)`.

There is no binding walk: V7 shows `ClearAllBindings` cannot reach template bindings (spec 4.3).

- [ ] **Step 1: Write the failing tests** (`Gui` unless marked).

```csharp
[Theory, InlineData("expiry"), InlineData("x"), InlineData("body"), InlineData("dismiss"), InlineData("eviction")]
Closed_PlainDataObjectWithTemplate_NotificationIsCollected(string path) // GcAssert.CollectedAsync on the Notification
[Fact] Closed_PlainDataObjectWithTemplate_SourceIsCollected()  // caller's POCO, after the caller drops it
[Fact] Closed_GeneratedTreeElement_IsCollected()               // V5
[Fact] Closed_CallerUIElement_KeepsBindings()
    // Show a TextBlock with a code-set binding; close; Assert.NotNull(BindingOperations.GetBindingExpression(block, TextBlock.TextProperty))
[Fact] Closed_CallerUIElement_CanBeShownAgain()                // second show of the same element displays
[Fact] Closed_CallerIconElement_KeepsBindings()                // Review Focus 1: element passed as Icon
[Fact] Closed_CallerButtonContentElement_KeepsBindings()       // Review Focus 1: element passed as LeftButtonContent
[Fact] Closing_ContentCommandStillBound_DuringAnimation()      // NotificationCloseInvoked handler: GetBindingExpression not null
[Fact] ClosingTime_DefaultTemplate_IsFourHundredMs()           // Assert.Equal(TimeSpan.FromMilliseconds(400), ...)
[Fact] ClosingTime_AutomaticChild_DoesNotThrow()               // headless
[Fact] ClosingTime_EmptyStoryboard_DoesNotThrow()              // headless
```

- [ ] **Step 2: Run them in the VM.** Record which fail. Expected to fail: `ClosingTime_AutomaticChild...`, `ClosingTime_EmptyStoryboard...`, and any collectability test the current reference graph breaks.

- [ ] **Step 3: Drop references in session step 7.** Set `Content = null` and `ContentTemplate = null`, and remove the control from the area's panel and session list. Add nothing that touches bindings.

- [ ] **Step 4: Implement `ComputeClosingAnimationTime`.** Over `BeginStoryboard` actions of the `NotificationCloseInvokedEvent` trigger, take each storyboard's and child's `Duration` where `HasTimeSpan`, add `BeginTime`, keep the maximum. The default template must stay 0.4 s; note its first storyboard's own `Duration` is shorter than its child.

- [ ] **Step 5: Run the tests in the VM.** Expected: PASS.
  - If a collectability test still fails, find the retention path first, for example with a dump and `gcroot`, and report it. Do not add a release mechanism without that path.
  - **V4:** add no storyboard handling unless the path shows storyboards as the cause.
  - If only `..._SourceIsCollected` fails while the `Notification` is collected, report it instead of fixing it: the caller's own object is outside the library's graph.

- [ ] **Step 6: Commit** `fix(wpf): drop notification content references after close`.

---

### Task 4: `NotificationViewModel`

**Files:**
- Create: `src/Notification.Wpf/ViewModels/NotificationViewModel.cs`
- Modify: `src/Notification.Wpf/Controls/Notifications/Notification.cs`, `PART_AttachButton.cs`, `src/Notification.Wpf/Controls/NotificationArea.cs`, `src/Notification.Wpf/Base/NotificationTemplateSelector.cs`, `src/Notification.Wpf/Converters/NotificationAttachVisibleConverter.cs`, `src/Notification.Wpf/Themes/Generic.xaml`
- Delete: `src/Notification.Wpf/Converters/ActionToCommandConverter.cs` if nothing uses it after this task
- Test: `Tests/Notification.Wpf.Tests/Lifecycle/NotificationViewModelTests.cs`

**Interfaces:**
- Consumes: `NotificationSession.RequestClose`, `PendingReason`, `PendingButtonId`, `PendingButtonIndex`.
- Produces (namespace `Notifications.Wpf.ViewModels`):

```csharp
public sealed class NotificationViewModel : ViewModel, ICustomizedNotification
{
    internal NotificationViewModel(NotificationContent source, NotificationSession session);
    public NotificationContent Source { get; }
    // copied at construction, same names as today: Title, Message, Type, Image, Icon, IconForeground,
    // Background, Foreground, TitleTextSettings, MessageTextSettings, TrimType, RowsCount,
    // LeftButtonContent, RightButtonContent
    public ICommand LeftButtonCommand { get; }   // null when the source has no action
    public ICommand RightButtonCommand { get; }
}
```

- [ ] **Step 1: Write the failing tests** (`Gui` unless marked).

```csharp
[Fact] LeftButton_Click_RunsActionAndReportsActionButton()  // real click via ButtonAutomationPeer; ButtonId "left", ButtonIndex 0
[Fact] RightButton_Click_RunsActionAndReportsActionButton() // "right", 1
[Fact] ShowDialogAsync_ButtonClick_ReturnsRealButtonId()    // Buttons[0].Id
[Fact] DoubleClick_RunsActionOnce()                         // Review Focus 3
[Fact] ThrowingAction_StillCloses()                         // Review Focus 3: Execute throws; session.IsClosing true; ShowAsync completes
[Fact] Show_DoesNotMutateCallerContent()                    // Assert.Same(originalAction, content.LeftButtonAction)
[Fact] SameContentShownTwice_FirstNotificationIsCollected() // chain epsilon
[Fact] SameContentInTwoAreasWithOneName_ButtonsAreIndependent() // Review Focus 2
[Fact] Closed_ViaActionButton_IsCollected()
[Fact] ViewModel_CopiesAllBoundProperties()                 // headless, every listed property
[Fact] Subclass_IsReachableThroughSource()                  // headless
[Fact] AttachButton_OpensContentWindow()
[Fact] AttachIfMoreRows_ComputesVisibility()                // converter accepts the view model
[Fact] CustomTemplate_CloseOnClick_StillRecordsCommandReason() // template keeps CloseOnClick plus Command
```

- [ ] **Step 2: Run them.** Expected: compile failure on `NotificationViewModel`. On `dev`, `Show_DoesNotMutateCallerContent` and `SameContentShownTwice...` are the defects being fixed.

- [ ] **Step 3: Implement the view model.** Use `LamdaCommand` from `Notifications.Wpf.Command`. Each command, in order: return if the session is closing; set `PendingButtonId` and `PendingButtonIndex` with `??=` (`"left"`, 0 / `"right"`, 1); `RequestClose(NotificationCloseReason.ActionButton)`; run the caller's action. This order keeps v11 behavior for a throwing action: close has started and the exception goes to the dispatcher.

- [ ] **Step 4: Wrap in the `Notification` constructor.** `Content = content is NotificationContent nc ? new NotificationViewModel(nc, Session) : content`. Remove the wrapper block from `NotificationArea.Show`. Body click and right click keep reading from the original `content`.

- [ ] **Step 5: Update the template.**
  - `DefaultNotificationTemplate` key type becomes the view model; add `xmlns:vm="clr-namespace:Notifications.Wpf.ViewModels"`.
  - `Command="{Binding LeftButtonCommand}"`; visibility binds the same path through the existing `IsNull` chain; same for the right button.
  - Remove `controls:Notification.CloseOnClick` from `LeftButton` and `RightButton` only.
  - Selector: `NotificationViewModel => _defaultNotificationTemplate`.
  - `AttachOnClickProperty` becomes `NotificationViewModel`, default `null`; update its changed handler.
  - `NotificationAttachVisibleConverter` accepts the view model.

- [ ] **Step 6: Run the tests.** Non-GUI on the host, `Gui` in the VM. Expected: PASS.

- [ ] **Step 7: Commit** `feat(wpf)!: show NotificationContent through a per-show view model`.

---

### Task 5: Progress and host window teardown

**Files:**
- Modify: `src/Notification.Wpf/Controls/NotificationArea.cs`, `src/Notification.Wpf/Classes/NotifierProgress.cs`, `src/Notification.Wpf/Controls/Notifications/NotificationSession.cs`
- Test: `Tests/Notification.Wpf.Tests/Lifecycle/ProgressTeardownTests.cs`

**Interfaces:**
- Consumes: `NotificationArea.CancelAll`, `NotificationSession.Token`.
- Produces: `internal void NotifierProgress<T>.ClearArea(Notification area)`: clears `Area` only if it is that control. `NotificationSession` gets `internal Action OnReleased { get; set; }`, run in step 7, so the progress show can register `ClearArea`.

- [ ] **Step 1: Write the failing tests** (`Gui`).

```csharp
[Fact] HostWindowClosed_ProgressLoopEnds_AndCancelProgressRuns()  // token cancelled; loop's Show task finished
[Fact] HostWindowClosed_ToastReportsCancelled()
[Fact] HostWindowClosed_NotificationIsCollected()
[Fact] AreaUnloadedByTabSwitch_KeepsNotificationsOpen()           // area in a TabControl; switch tab and back; session alive
[Fact] ProgressClosed_NotifierProgressNoLongerHoldsControl()      // keep the progress object alive; control collected
[Fact] ProgressDispose_ClosesNotificationOnce()
```

- [ ] **Step 2: Run them in the VM.** Expected: the host-window and retention tests FAIL.

- [ ] **Step 3: Implement.**
  - Polling uses a token source linked from `progress.Cancel.Token` and `session.Token`.
  - On `Loaded`, `NotificationArea` subscribes to `Closed` of `Window.GetWindow(this)`, replacing the subscription if the window changed. On `Closed` it unsubscribes and calls `CancelAll(NotificationCloseReason.Cancelled)`. `Unloaded` still only calls `RemoveArea`.
  - `Loaded` must call `AddArea` only if the area is not already registered: tab switches load it repeatedly.

- [ ] **Step 4: Run the tests in the VM.** Expected: PASS.

- [ ] **Step 5: Commit** `fix(wpf): end sessions and progress polling when the host window closes`.

---

### Task 6: Overlay lifetime

**Files:**
- Create: `src/Notification.Wpf/Base/HostWindowWatcher.cs`
- Modify: `src/Notification.Wpf/Base/NotificationManager.cs`, `src/Notification.Wpf/Controls/Notifications/NotificationSession.cs`, `src/Notification.Wpf/Controls/Notifications/PART_CloseButton.cs`
- Delete from `src/Notification.Wpf/Utils/VisualTreeHelperExtensions.cs`: `GetActiveNotificationCount`, `GetControlsList` and the static `_activeControls`, if unused elsewhere
- Test: `Tests/Notification.Wpf.Tests/Lifecycle/HostWindowWatcherTests.cs`, `OverlayLifetimeTests.cs`

**Interfaces:**
- Consumes: `NotificationArea.ActiveSessions`, `CancelAll`.
- Produces:

```csharp
internal sealed class HostWindowWatcher
{
    internal HostWindowWatcher(Func<IReadOnlyList<Window>> getHostWindows,
                               Func<bool> shouldReactToLastWindow,
                               Action onLastHostWindowClosed);
    internal void Scan();              // subscribe Closed on untracked windows
    internal int TrackedCount { get; }
}
// NotificationManager:
internal static void OnSessionReleased(NotificationArea area); // closes _window if it hosts area and no session remains
```

`getHostWindows` returns `Application.Current.Windows` without the overlay; it returns an empty list when `Application.Current` is null or its dispatcher is not the current one. `shouldReactToLastWindow` is `Application.Current?.ShutdownMode == ShutdownMode.OnLastWindowClose`.

- [ ] **Step 1: Write the failing tests** (`Gui`; the watcher receives a test list, so no `Application` is needed).

```csharp
[Fact] LastTrackedWindowClosed_InvokesCallbackOnce()
[Fact] TrackedWindowClosed_OtherWindowOpen_DoesNotInvoke()
[Fact] WindowOpenedAfterScan_IsTrackedAfterAnotherCloses()  // the invariant in spec 4.4
[Fact] NotLastWindowMode_DoesNotInvoke()                    // shouldReactToLastWindow false
[Fact] Closed_UnsubscribesFromWindow()                      // TrackedCount drops; window collected
[Fact] Overlay_LastSessionEnds_WindowCloses()               // _window closed, not hidden: Assert.False(window.IsLoaded)
[Fact] Overlay_NextShowAfterClose_CreatesNewWindow()
[Fact] Overlay_ShowDuringLastClose_KeepsToast()             // Review Focus 5
[Fact] Overlay_CallbackCancelsSessions_ShowAsyncCompletes() // reason Cancelled
```

- [ ] **Step 2: Run them in the VM.** Expected: watcher tests do not compile; overlay tests that exist on `dev` show which behaviors already hold.

- [ ] **Step 3: Implement `HostWindowWatcher`.** When a tracked window closes: unsubscribe, remove it, `Scan()`. If nothing is tracked and `getHostWindows()` is empty and `shouldReactToLastWindow()` is true, invoke the callback.

- [ ] **Step 4: Wire the overlay.**
  - In `ShowContent`, after the overlay is shown, call `Scan()` on one static watcher.
  - Right after `new NotificationsOverlayWindow`: if `Application.Current?.MainWindow` is the overlay, set it to `null`.
  - Callback: `CancelAll(NotificationCloseReason.Cancelled)` on the overlay's area.
  - Session step 8 calls `NotificationManager.OnSessionReleased(area)`, replacing the title lookup and visual scan in `PART_CloseButton.cs`.
  - `OnSessionReleased` checks `ActiveSessions` at that moment, so a toast added during the animation keeps the window open.

- [ ] **Step 5: Run the tests.** Non-GUI on the host, `Gui` in the VM. Expected: PASS.

- [ ] **Step 6: Commit** `fix(wpf): close the overlay with its last session and last host window`.

---

### Task 7: Exit matrix in the VM

**Files:**
- Create: `Tests/Notification.Wpf.ExitHost/Notification.Wpf.ExitHost.csproj` (`WinExe`, `net10.0-windows`, `UseWPF`, `IsPackable=false`), `App.cs`, `run-exit-matrix.ps1`
- Modify: `Notification.Wpf.sln`

**Interfaces:**
- Consumes: public library API only.
- Produces: `Notification.Wpf.ExitHost.exe --mode <OnLastWindowClose|OnMainWindowClose|OnExplicitShutdown> --overlay-first <true|false> --toast <idle|active|progress> --second-window <true|false> --log <path>`, reused by Task 8. The host closes its windows on a schedule and writes `ASYNC_DONE <reason>` and `EXIT` lines to the log.

- [ ] **Step 1: Build the host and the script.** The script runs each case, waits for the exit defined in spec 6.3 and prints `PASS` or `FAIL` per case. Cover every value of every dimension, plus: overlay created before `MainWindow` is assigned (PR #89 regression 2), and progress running with a second window open (regression 3).

- [ ] **Step 2: Add the two separate-process claims** as cases `--claim v3` and `--claim v6`, printing the observed result: whether `MainWindow` could be reset, and whether closing the only window before any host window exists shuts the application down.

- [ ] **Step 3: Run the matrix in the VM.** Run: `run-exit-matrix.ps1`. Expected: every case `PASS`. Pass thresholds are in spec 6.3.

- [ ] **Step 4: Act on V3 and V6.** If the reset in Task 6 does not work, remove it and add the hazard to `specs/roadmap.md`. Add the V6 result to `specs/roadmap.md`.

- [ ] **Step 5: Commit** `test(wpf): add process exit matrix`.

---

### Task 8: Soak run

**Files:**
- Modify: `Tests/Notification.Wpf.ExitHost/App.cs`

**Interfaces:**
- Consumes: the host from Task 7.
- Produces: `--soak --minutes <n> --dump-marker <path>`, with the load from spec 6.3.

- [ ] **Step 1: Add the soak mode.** About 5 notifications per second, mixing plain text, action buttons, a plain object with a `DataTemplate`, repeated shows of one `NotificationContent`, and periodic progress. Pause the load, wait for all notifications to close, force GC and write the marker at minute 10 and at the end.

- [ ] **Step 2: Run a 2-minute smoke in the VM.** Expected: no crash, marker written.

- [ ] **Step 3: Run `--minutes 60` in the VM,** taking a dump at each marker. Compare with `dotnet-dump`: `dumpheap -stat -type Notification.Wpf.Controls.Notification` and `eeheap -gc`. Expected: no closed `Notification` instance; end heap within 10% of the 10-minute dump.

- [ ] **Step 4: Record the numbers in the PR body draft and commit** `test(wpf): add soak mode to the exit host`.

---

### Task 9: Release documents and version

**Files:**
- Modify: `Migration.md`, `Updates.md`, `README.md`, `Documentation.md`, `specs/roadmap.md`, `src/Notification.Wpf/Notification.Wpf.csproj`, and the `<Version>` line of `src/Notification.Core`, `Notification.Avalonia`, `Notification.Maui`, `Notification.Console` projects
- Modify if the build regenerates it: `src/Notification.Wpf/Notification.Wpf.xml`
- Check: `Samples/Notification.Wpf.Sample`

- [ ] **Step 1: Set every package to `12.0.0.0`.** Update `PackageReleaseNotes` in the WPF project.

- [ ] **Step 2: Write `Migration.md`.** Add a v12 section with all seven rows of spec section 5, each with a before and after snippet. The current file opens by promising full backward compatibility for 10.0; keep that scoped to 10.0.

- [ ] **Step 3: Write `Updates.md`.** Add `## 12.0.0` in the existing format, with breaking changes first, and credit `luoye1314` and PR #89 for the report.

- [ ] **Step 4: Update the rest.** Remove the `ShutdownMode` workaround from README Known Issues. In `specs/roadmap.md`, remove the shared-`NotificationContent` limitation. Fix any `NotificationContent` template references in `Documentation.md`.

- [ ] **Step 5: Build everything.** Run: `dotnet build Notification.Wpf.sln -c Release`. Expected: 0 errors. If MAUI workloads are missing locally, build the projects listed in `.github/workflows/tests.yml` instead and say which were skipped.

- [ ] **Step 6: Smoke the sample in the VM.** Show text, buttons, progress and the custom string template; confirm it exits on close.

- [ ] **Step 7: Commit** `docs: add v12 migration guide and bump versions to 12.0.0.0`.

---

### Task 10: Final gate

**Files:** none, unless a finding needs a fix.

- [ ] **Step 1: Run all tests.** Host: `--filter "Category!=Gui"`. VM: `--filter "Category=Gui"`, exit matrix. Expected: PASS.

- [ ] **Step 2: Compare against PR #89's defects.** Confirm these tests exist and pass: `LeftButton_Click_...`, `Closed_CallerUIElement_KeepsBindings`, the overlay-first exit case, `HostWindowClosed_ProgressLoopEnds_...`.

- [ ] **Step 3: Run `cold-diff-review` on the branch against `dev`.** Revert the hunk for every finding, per spec 6.4. Fix confirmed findings, then run its second pass.

- [ ] **Step 4: Stop and report.** Do not push, open the PR, or close PR #89 without the owner's word. Report the test results, soak numbers, and anything skipped.

- [ ] **Step 5: After the owner approves the push, check CI.** `.github/workflows/tests.yml` runs the whole WPF test project on `windows-latest`, `Gui` tests included. If they cannot show windows there, add `--filter "Category!=Gui"` to that step and keep the VM run as the gate, per spec 6.2.
