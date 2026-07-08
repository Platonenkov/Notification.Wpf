# Async Show API with Close Reason — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `INotificationService.ShowAsync` and `ShowDialogAsync` returning `Task<NotificationCloseResult>` with a structured close reason (Timeout / BodyClick / XButton / ActionButton+ButtonId / Cancelled) across all four supported platforms (WPF, Avalonia, MAUI, Console), introduce an N-button `Buttons[]` model on `NotificationRequest`, deprecate the legacy `LeftButton*` / `RightButton*` fields, and update docs and all sample projects. Release v11.0.0.

**Architecture:** Public types and the interface extension live in `Notification.Core`. Each platform implementation routes lifecycle events through a single point that completes a shared internal `NotificationCompletionSource`. The legacy two-button fields are translated to `NotificationButton`s on the way in via `NotificationButtonResolver`. WPF retains routed-event semantics by enriching `RoutedEventArgs` rather than changing the event delegate type.

**Tech Stack:** .NET 10 / 9 / 8 / 4.8 (multi-targeted via `Notification.Wpf.csproj`); C# `LangVersion=latest`; xUnit v3 for tests; existing build via solution `Notification.Wpf.sln`.

**Branch:** `claude/async-show-close-reason-9a8660`

**Spec:** [specs/2026-06-08-async-show-with-close-reason-design.md](../specs/2026-06-08-async-show-with-close-reason-design.md)

---

## File Structure

### New files (Notification.Core)

| Path | Responsibility |
|---|---|
| `src/Notification.Core/Models/NotificationCloseReason.cs` | `enum NotificationCloseReason` |
| `src/Notification.Core/Models/NotificationCloseResult.cs` | `sealed class NotificationCloseResult` |
| `src/Notification.Core/Models/NotificationButton.cs` | `sealed class NotificationButton` |
| `src/Notification.Core/Models/NotificationButtonRole.cs` | `enum NotificationButtonRole` |
| `src/Notification.Core/Models/NotificationDialogFallback.cs` | `enum NotificationDialogFallback` |
| `src/Notification.Core/Internal/NotificationButtonResolver.cs` | internal helper — legacy → `Buttons[]` |
| `src/Notification.Core/Internal/NotificationCompletionSource.cs` | internal helper — `TaskCompletionSource` + CT wiring |
| `src/Notification.Core/Extensions/NotificationServiceAsyncExtensions.cs` | convenience `ShowAsync(string, ...)` overloads |

### New files (WPF)

| Path | Responsibility |
|---|---|
| `src/Notification.Wpf/Controls/Notifications/NotificationClosedRoutedEventArgs.cs` | `RoutedEventArgs` subclass with reason and button info |

### New files (tests)

| Path | Responsibility |
|---|---|
| `Tests/Notification.Core.Tests/NotificationCloseResultTests.cs` | unit tests for the result type |
| `Tests/Notification.Core.Tests/NotificationButtonResolverTests.cs` | unit tests for the legacy converter |
| `Tests/Notification.Core.Tests/NotificationServiceAsyncExtensionsTests.cs` | unit tests for the convenience extensions |
| `Tests/Notification.Wpf.Tests/NotificationManagerAsyncTests.cs` | behavioral tests for WPF `ShowAsync` / `ShowDialogAsync` |
| `Tests/Notification.Console.Tests/ConsoleNotificationManagerAsyncTests.cs` | behavioral tests for Console |

### New files (docs / roadmap)

| Path | Responsibility |
|---|---|
| `specs/roadmap.md` | post-v11 follow-up roadmap, including the locked `RunWithProgressAsync` shape from spec §12.1 |

### Modified files

| Path | Reason |
|---|---|
| `src/Notification.Core/Interfaces/INotificationService.cs` | add `ShowAsync` / `ShowDialogAsync` |
| `src/Notification.Core/Models/NotificationRequest.cs` | add `Buttons`, mark legacy fields `[Obsolete]` |
| `src/Notification.Core/Events/NotificationLifecycleEventArgs.cs` | add nullable `CloseReason` + new constructor |
| `src/Notification.Wpf/Controls/Notifications/PART_CloseButton.cs` | raise enriched event args from `Close()` |
| `src/Notification.Wpf/Controls/Notifications/Notification.cs` (or new partial) | internal `CloseReason` / `ClickedButtonId` / `ClickedButtonIndex` hint fields |
| `src/Notification.Wpf/Controls/NotificationArea.cs` | set close hints in click / X / timeout paths |
| `src/Notification.Wpf/Base/NotificationManager.cs` | implement `ShowAsync` / `ShowDialogAsync` |
| `src/Notification.Avalonia/AvaloniaNotificationManager.cs` | implement `ShowAsync` / `ShowDialogAsync` |
| `src/Notification.Avalonia/Controls/AvaloniaNotificationHost.*` (path TBD by reading repo) | propagate close reason |
| `src/Notification.Maui/MauiNotificationManager.cs` | implement `ShowAsync` / `ShowDialogAsync` |
| `src/Notification.Console/ConsoleNotificationManager.cs` | implement `ShowAsync` / `ShowDialogAsync` |
| `src/Notification.Wpf/Notification.Wpf.csproj` | bump `<Version>` to `11.0.0.0` |
| `src/Notification.Core/Notification.Core.csproj` | bump version if applicable |
| `src/Notification.Avalonia/Notification.Avalonia.csproj` | bump version |
| `src/Notification.Maui/Notification.Maui.csproj` | bump version |
| `src/Notification.Console/Notification.Console.csproj` | bump version |
| `README.md`, `Documentation.md`, `Migration.md`, `Updates.md` | docs |
| `Samples/Notification.Wpf.Sample/**` | add async demo page |
| `Samples/Notification.Avalonia.Sample/**` | add async demo page |
| `Samples/Notification.Maui.Sample/**` | add async demo page |
| `Samples/Notification.Console.Sample/**` | add async CLI flow |

---

## Stage 1: Core types

### Task 1.1: `NotificationCloseReason` enum

**Files:**
- Create: `src/Notification.Core/Models/NotificationCloseReason.cs`

- [ ] **Step 1: Write the failing test**

Create `Tests/Notification.Core.Tests/NotificationCloseResultTests.cs`:

```csharp
using Notification.Core;
using Xunit;

namespace Notification.Core.Tests;

public class NotificationCloseReasonTests
{
    [Fact]
    public void Enum_has_expected_five_values()
    {
        Assert.Equal(0, (int)NotificationCloseReason.Timeout);
        Assert.Equal(1, (int)NotificationCloseReason.BodyClick);
        Assert.Equal(2, (int)NotificationCloseReason.XButton);
        Assert.Equal(3, (int)NotificationCloseReason.ActionButton);
        Assert.Equal(4, (int)NotificationCloseReason.Cancelled);
    }
}
```

- [ ] **Step 2: Run test to verify it fails (build error: type not found)**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationCloseReasonTests"`
Expected: BUILD FAIL — `NotificationCloseReason` not found.

- [ ] **Step 3: Implement the enum**

Create `src/Notification.Core/Models/NotificationCloseReason.cs`:

```csharp
namespace Notification.Core
{
    /// <summary>
    /// Describes why a notification was closed.
    /// </summary>
    public enum NotificationCloseReason
    {
        /// <summary>The expiration time elapsed.</summary>
        Timeout = 0,
        /// <summary>The user clicked the notification body while <see cref="NotificationRequest.CloseOnClick"/> was true.</summary>
        BodyClick = 1,
        /// <summary>The user pressed the close (X) button.</summary>
        XButton = 2,
        /// <summary>The user pressed one of the action buttons.</summary>
        ActionButton = 3,
        /// <summary>The notification was cancelled programmatically via <see cref="System.Threading.CancellationToken"/> or <see cref="INotificationService.Dismiss"/>.</summary>
        Cancelled = 4
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationCloseReasonTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Models/NotificationCloseReason.cs Tests/Notification.Core.Tests/NotificationCloseResultTests.cs
git commit -m "feat(core): add NotificationCloseReason enum"
```

---

### Task 1.2: `NotificationCloseResult` class

**Files:**
- Create: `src/Notification.Core/Models/NotificationCloseResult.cs`
- Modify: `Tests/Notification.Core.Tests/NotificationCloseResultTests.cs`

- [ ] **Step 1: Add failing tests to the existing test file**

Append to `Tests/Notification.Core.Tests/NotificationCloseResultTests.cs`:

```csharp
public class NotificationCloseResultTests
{
    [Fact]
    public void IsCancelled_returns_true_only_when_reason_is_cancelled()
    {
        Assert.True(new NotificationCloseResult { Reason = NotificationCloseReason.Cancelled }.IsCancelled);
        Assert.False(new NotificationCloseResult { Reason = NotificationCloseReason.Timeout }.IsCancelled);
        Assert.False(new NotificationCloseResult { Reason = NotificationCloseReason.ActionButton }.IsCancelled);
    }

    [Fact]
    public void Defaults_are_safe()
    {
        NotificationCloseResult r = new();
        Assert.Equal(System.Guid.Empty, r.NotificationId);
        Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
        Assert.Null(r.ButtonId);
        Assert.Null(r.ButtonIndex);
        Assert.False(r.IsCancelled);
    }

    [Fact]
    public void Init_setters_populate_fields()
    {
        System.Guid id = System.Guid.NewGuid();
        NotificationCloseResult r = new()
        {
            NotificationId = id,
            Reason = NotificationCloseReason.ActionButton,
            ButtonId = "ok",
            ButtonIndex = 0
        };
        Assert.Equal(id, r.NotificationId);
        Assert.Equal(NotificationCloseReason.ActionButton, r.Reason);
        Assert.Equal("ok", r.ButtonId);
        Assert.Equal(0, r.ButtonIndex);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail (type not found)**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationCloseResultTests"`
Expected: BUILD FAIL — `NotificationCloseResult` not found.

- [ ] **Step 3: Implement the class**

Create `src/Notification.Core/Models/NotificationCloseResult.cs`:

```csharp
using System;

namespace Notification.Core
{
    /// <summary>
    /// Result of an asynchronous notification or dialog close.
    /// </summary>
    public sealed class NotificationCloseResult
    {
        /// <summary>The unique identifier of the closed notification.</summary>
        public Guid NotificationId { get; init; }

        /// <summary>Why the notification was closed.</summary>
        public NotificationCloseReason Reason { get; init; }

        /// <summary>The identifier of the action button that closed the notification, or <c>null</c>.</summary>
        public string ButtonId { get; init; }

        /// <summary>The zero-based index of the action button that closed the notification, or <c>null</c>.</summary>
        public int? ButtonIndex { get; init; }

        /// <summary>True when <see cref="Reason"/> is <see cref="NotificationCloseReason.Cancelled"/>.</summary>
        public bool IsCancelled => Reason == NotificationCloseReason.Cancelled;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationCloseResultTests"`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Models/NotificationCloseResult.cs Tests/Notification.Core.Tests/NotificationCloseResultTests.cs
git commit -m "feat(core): add NotificationCloseResult"
```

---

### Task 1.3: `NotificationButtonRole` enum

**Files:**
- Create: `src/Notification.Core/Models/NotificationButtonRole.cs`

- [ ] **Step 1: Add failing test**

Create `Tests/Notification.Core.Tests/NotificationButtonTests.cs`:

```csharp
using Notification.Core;
using Xunit;

namespace Notification.Core.Tests;

public class NotificationButtonRoleTests
{
    [Fact]
    public void Enum_has_four_expected_values()
    {
        Assert.Equal(0, (int)NotificationButtonRole.Default);
        Assert.Equal(1, (int)NotificationButtonRole.Affirmative);
        Assert.Equal(2, (int)NotificationButtonRole.Negative);
        Assert.Equal(3, (int)NotificationButtonRole.Destructive);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationButtonRoleTests"`
Expected: BUILD FAIL.

- [ ] **Step 3: Implement enum**

Create `src/Notification.Core/Models/NotificationButtonRole.cs`:

```csharp
namespace Notification.Core
{
    /// <summary>
    /// Hint used by platform UI to style action buttons. Does not affect the
    /// <see cref="NotificationCloseReason"/> returned on click.
    /// </summary>
    public enum NotificationButtonRole
    {
        /// <summary>No special role.</summary>
        Default = 0,
        /// <summary>Confirmation (e.g. OK, Yes, Save).</summary>
        Affirmative = 1,
        /// <summary>Refusal or cancel (e.g. Cancel, No).</summary>
        Negative = 2,
        /// <summary>Destructive action (e.g. Delete). Platforms may render this as red.</summary>
        Destructive = 3
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationButtonRoleTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Models/NotificationButtonRole.cs Tests/Notification.Core.Tests/NotificationButtonTests.cs
git commit -m "feat(core): add NotificationButtonRole enum"
```

---

### Task 1.4: `NotificationButton` class

**Files:**
- Create: `src/Notification.Core/Models/NotificationButton.cs`
- Modify: `Tests/Notification.Core.Tests/NotificationButtonTests.cs`

- [ ] **Step 1: Add failing tests**

Append to `Tests/Notification.Core.Tests/NotificationButtonTests.cs`:

```csharp
public class NotificationButtonClassTests
{
    [Fact]
    public void Default_role_is_default()
    {
        NotificationButton b = new() { Id = "ok", Content = "OK" };
        Assert.Equal(NotificationButtonRole.Default, b.Role);
    }

    [Fact]
    public void Init_setters_populate_fields()
    {
        NotificationButton b = new()
        {
            Id = "delete",
            Content = "Delete",
            Role = NotificationButtonRole.Destructive
        };
        Assert.Equal("delete", b.Id);
        Assert.Equal("Delete", b.Content);
        Assert.Equal(NotificationButtonRole.Destructive, b.Role);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationButtonClassTests"`
Expected: BUILD FAIL — `NotificationButton` not found.

- [ ] **Step 3: Implement class**

Create `src/Notification.Core/Models/NotificationButton.cs`:

```csharp
namespace Notification.Core
{
    /// <summary>
    /// An action button shown on a notification or dialog.
    /// </summary>
    public sealed class NotificationButton
    {
        /// <summary>Stable identifier. Returned in <see cref="NotificationCloseResult.ButtonId"/>. Required and unique within a request.</summary>
        public string Id { get; init; }

        /// <summary>User-visible text on the button.</summary>
        public string Content { get; init; }

        /// <summary>Role hint for platform styling.</summary>
        public NotificationButtonRole Role { get; init; } = NotificationButtonRole.Default;
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationButtonClassTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Models/NotificationButton.cs Tests/Notification.Core.Tests/NotificationButtonTests.cs
git commit -m "feat(core): add NotificationButton"
```

---

### Task 1.5: `NotificationDialogFallback` enum

**Files:**
- Create: `src/Notification.Core/Models/NotificationDialogFallback.cs`

- [ ] **Step 1: Add failing test**

Create `Tests/Notification.Core.Tests/NotificationDialogFallbackTests.cs`:

```csharp
using Notification.Core;
using Xunit;

namespace Notification.Core.Tests;

public class NotificationDialogFallbackTests
{
    [Fact]
    public void Enum_has_three_expected_values()
    {
        Assert.Equal(0, (int)NotificationDialogFallback.ShowFirstAffirmative);
        Assert.Equal(1, (int)NotificationDialogFallback.SkipSilently);
        Assert.Equal(2, (int)NotificationDialogFallback.Throw);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationDialogFallbackTests"`
Expected: BUILD FAIL.

- [ ] **Step 3: Implement enum**

Create `src/Notification.Core/Models/NotificationDialogFallback.cs`:

```csharp
namespace Notification.Core
{
    /// <summary>
    /// Behavior when a platform cannot render all buttons of a dialog.
    /// </summary>
    public enum NotificationDialogFallback
    {
        /// <summary>Render the first <see cref="NotificationButtonRole.Affirmative"/> button (or the first button overall when none Affirmative); drop the rest.</summary>
        ShowFirstAffirmative = 0,
        /// <summary>Do not render anything; complete the Task with <see cref="NotificationCloseReason.Timeout"/> immediately.</summary>
        SkipSilently = 1,
        /// <summary>Throw <see cref="System.NotSupportedException"/> synchronously before returning the Task.</summary>
        Throw = 2
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationDialogFallbackTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Models/NotificationDialogFallback.cs Tests/Notification.Core.Tests/NotificationDialogFallbackTests.cs
git commit -m "feat(core): add NotificationDialogFallback enum"
```

---

## Stage 2: `NotificationRequest` extension

### Task 2.1: Add `Buttons` collection property

**Files:**
- Modify: `src/Notification.Core/Models/NotificationRequest.cs`

- [ ] **Step 1: Add failing test**

Append to `Tests/Notification.Core.Tests/NotificationRequestTests.cs` (if file exists) or create a new `Buttons_property_tests` class there:

```csharp
[Fact]
public void Buttons_defaults_to_null()
{
    NotificationRequest r = new();
    Assert.Null(r.Buttons);
}

[Fact]
public void Buttons_setter_stores_collection()
{
    NotificationButton[] buttons = new[]
    {
        new NotificationButton { Id = "ok", Content = "OK", Role = NotificationButtonRole.Affirmative }
    };
    NotificationRequest r = new() { Buttons = buttons };
    Assert.Single(r.Buttons);
    Assert.Equal("ok", r.Buttons[0].Id);
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "Buttons_"`
Expected: BUILD FAIL — `Buttons` property does not exist.

- [ ] **Step 3: Add property to `NotificationRequest.cs`**

Add to `src/Notification.Core/Models/NotificationRequest.cs` after the `RightButtonContent` block (around line 109):

```csharp
        /// <summary>
        /// Gets or sets the list of action buttons attached to the notification.
        /// When set, this replaces the obsolete <see cref="LeftButtonContent"/> /
        /// <see cref="RightButtonContent"/> pair. Required for
        /// <see cref="INotificationService.ShowDialogAsync"/>.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<NotificationButton> Buttons { get; set; }
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "Buttons_"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Models/NotificationRequest.cs Tests/Notification.Core.Tests/NotificationRequestTests.cs
git commit -m "feat(core): add Buttons collection to NotificationRequest"
```

---

### Task 2.2: Mark legacy button fields `[Obsolete]`

**Files:**
- Modify: `src/Notification.Core/Models/NotificationRequest.cs` (lines 92–109)

- [ ] **Step 1: Add failing test that asserts the attribute is present**

Append to `Tests/Notification.Core.Tests/NotificationRequestTests.cs`:

```csharp
[Fact]
public void Legacy_button_fields_are_marked_obsolete()
{
    System.Type t = typeof(NotificationRequest);
    foreach (string name in new[] { nameof(NotificationRequest.LeftButtonAction),
                                    nameof(NotificationRequest.LeftButtonContent),
                                    nameof(NotificationRequest.RightButtonAction),
                                    nameof(NotificationRequest.RightButtonContent) })
    {
        System.Reflection.PropertyInfo p = t.GetProperty(name);
        Assert.NotNull(p);
        System.ObsoleteAttribute attr = (System.ObsoleteAttribute)System.Attribute.GetCustomAttribute(p, typeof(System.ObsoleteAttribute));
        Assert.NotNull(attr);
        Assert.Contains("Buttons", attr.Message);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "Legacy_button_fields_are_marked_obsolete"`
Expected: FAIL — attribute not present.

- [ ] **Step 3: Mark the four properties**

In `src/Notification.Core/Models/NotificationRequest.cs`, prefix each of `LeftButtonAction`, `LeftButtonContent`, `RightButtonAction`, `RightButtonContent` with:

```csharp
        [System.Obsolete("Use Buttons collection instead. Will be removed in v12.")]
```

- [ ] **Step 4: Run to verify pass and check the rest of the build**

Run: `dotnet build src/Notification.Core/Notification.Core.csproj`
Expected: succeeds with no errors. (Other projects in the solution may show CS0618 warnings — that is expected; they are addressed in Stage 6+ when the platform managers stop reading legacy fields directly.)

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "Legacy_button_fields_are_marked_obsolete"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Models/NotificationRequest.cs Tests/Notification.Core.Tests/NotificationRequestTests.cs
git commit -m "feat(core)!: mark NotificationRequest.LeftButton*/RightButton* as Obsolete (v12 removal)"
```

---

## Stage 3: Core internal helpers

### Task 3.1: `NotificationButtonResolver`

**Files:**
- Create: `src/Notification.Core/Internal/NotificationButtonResolver.cs`
- Create: `Tests/Notification.Core.Tests/NotificationButtonResolverTests.cs`

The resolver and tests must access `internal` from the test assembly. Add `InternalsVisibleTo` for `Notification.Core.Tests` in `src/Notification.Core/Notification.Core.csproj` first if not already present.

- [ ] **Step 1: Check / add `InternalsVisibleTo`**

Open `src/Notification.Core/Notification.Core.csproj`. If `<InternalsVisibleTo Include="Notification.Core.Tests" />` is not present inside an `<ItemGroup>`, add:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="Notification.Core.Tests" />
  </ItemGroup>
```

- [ ] **Step 2: Write failing tests**

Create `Tests/Notification.Core.Tests/NotificationButtonResolverTests.cs`:

```csharp
using System.Collections.Generic;
using Notification.Core;
using Notification.Core.Internal;
using Xunit;

namespace Notification.Core.Tests;

public class NotificationButtonResolverTests
{
    [Fact]
    public void Returns_explicit_buttons_when_provided()
    {
        NotificationRequest req = new()
        {
            Buttons = new[]
            {
                new NotificationButton { Id = "yes", Content = "Yes", Role = NotificationButtonRole.Affirmative },
                new NotificationButton { Id = "no",  Content = "No",  Role = NotificationButtonRole.Negative }
            }
        };

        IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

        Assert.Equal(2, result.Count);
        Assert.Equal("yes", result[0].Id);
    }

    [Fact]
    public void Converts_legacy_left_content_to_button_with_synthetic_id()
    {
        NotificationRequest req = new();
#pragma warning disable CS0618
        req.LeftButtonContent = "OK";
#pragma warning restore CS0618

        IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

        Assert.Single(result);
        Assert.Equal("left", result[0].Id);
        Assert.Equal("OK", result[0].Content);
        Assert.Equal(NotificationButtonRole.Affirmative, result[0].Role);
    }

    [Fact]
    public void Converts_legacy_right_content_to_button_with_synthetic_id()
    {
        NotificationRequest req = new();
#pragma warning disable CS0618
        req.RightButtonContent = "Cancel";
#pragma warning restore CS0618

        IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

        Assert.Single(result);
        Assert.Equal("right", result[0].Id);
        Assert.Equal(NotificationButtonRole.Negative, result[0].Role);
    }

    [Fact]
    public void Converts_both_legacy_fields_in_left_then_right_order()
    {
        NotificationRequest req = new();
#pragma warning disable CS0618
        req.LeftButtonContent = "OK";
        req.RightButtonContent = "Cancel";
#pragma warning restore CS0618

        IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

        Assert.Equal(2, result.Count);
        Assert.Equal("left", result[0].Id);
        Assert.Equal("right", result[1].Id);
    }

    [Fact]
    public void Returns_empty_when_neither_buttons_nor_legacy_populated()
    {
        NotificationRequest req = new();

        IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

        Assert.Empty(result);
    }

    [Fact]
    public void Buttons_collection_takes_priority_over_legacy_fields()
    {
        NotificationRequest req = new()
        {
            Buttons = new[] { new NotificationButton { Id = "primary", Content = "Primary" } }
        };
#pragma warning disable CS0618
        req.LeftButtonContent = "Ignored";
#pragma warning restore CS0618

        IReadOnlyList<NotificationButton> result = NotificationButtonResolver.ResolveButtons(req);

        Assert.Single(result);
        Assert.Equal("primary", result[0].Id);
    }
}
```

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationButtonResolverTests"`
Expected: BUILD FAIL — namespace `Notification.Core.Internal` not found.

- [ ] **Step 4: Implement the resolver**

Create `src/Notification.Core/Internal/NotificationButtonResolver.cs`:

```csharp
using System.Collections.Generic;

namespace Notification.Core.Internal
{
    /// <summary>
    /// Converts a <see cref="NotificationRequest"/>'s button surface
    /// (new <see cref="NotificationRequest.Buttons"/> or the obsolete
    /// <c>LeftButton*</c>/<c>RightButton*</c> pair) to a unified list.
    /// </summary>
    internal static class NotificationButtonResolver
    {
        /// <summary>
        /// Returns the buttons declared on the request. When <see cref="NotificationRequest.Buttons"/>
        /// is non-empty it is returned verbatim. Otherwise the obsolete legacy fields are
        /// mapped to <see cref="NotificationButton"/>s with synthetic ids ("left", "right").
        /// </summary>
        public static IReadOnlyList<NotificationButton> ResolveButtons(NotificationRequest request)
        {
            if (request.Buttons is { Count: > 0 })
                return request.Buttons;

            List<NotificationButton> result = new List<NotificationButton>(2);

#pragma warning disable CS0618
            if (!string.IsNullOrEmpty(request.LeftButtonContent))
                result.Add(new NotificationButton
                {
                    Id = "left",
                    Content = request.LeftButtonContent,
                    Role = NotificationButtonRole.Affirmative
                });

            if (!string.IsNullOrEmpty(request.RightButtonContent))
                result.Add(new NotificationButton
                {
                    Id = "right",
                    Content = request.RightButtonContent,
                    Role = NotificationButtonRole.Negative
                });
#pragma warning restore CS0618

            return result;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify pass**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationButtonResolverTests"`
Expected: PASS (6 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Notification.Core/Internal/NotificationButtonResolver.cs Tests/Notification.Core.Tests/NotificationButtonResolverTests.cs src/Notification.Core/Notification.Core.csproj
git commit -m "feat(core): internal NotificationButtonResolver for legacy → Buttons mapping"
```

---

### Task 3.2: `NotificationCompletionSource`

**Files:**
- Create: `src/Notification.Core/Internal/NotificationCompletionSource.cs`
- Create: `Tests/Notification.Core.Tests/NotificationCompletionSourceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Tests/Notification.Core.Tests/NotificationCompletionSourceTests.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Notification.Core;
using Notification.Core.Internal;
using Xunit;

namespace Notification.Core.Tests;

public class NotificationCompletionSourceTests
{
    private sealed class FakeService : INotificationService
    {
        public int DismissCallCount { get; private set; }
        public Guid DismissedId { get; private set; }
        public Guid Show(NotificationRequest request) => Guid.Empty;
        public void Dismiss(Guid id) { DismissCallCount++; DismissedId = id; }
        public void DismissAll() { }
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest req, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest req,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken ct = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Complete_sets_reason_button_and_id()
    {
        Guid id = Guid.NewGuid();
        FakeService svc = new();
        NotificationCompletionSource src = new(id, svc, CancellationToken.None);

        src.Complete(NotificationCloseReason.ActionButton, "ok", 0);

        NotificationCloseResult r = await src.Task;
        Assert.Equal(id, r.NotificationId);
        Assert.Equal(NotificationCloseReason.ActionButton, r.Reason);
        Assert.Equal("ok", r.ButtonId);
        Assert.Equal(0, r.ButtonIndex);
    }

    [Fact]
    public async Task Cancellation_completes_with_cancelled_and_calls_dismiss()
    {
        Guid id = Guid.NewGuid();
        FakeService svc = new();
        CancellationTokenSource cts = new();
        NotificationCompletionSource src = new(id, svc, cts.Token);

        cts.Cancel();

        NotificationCloseResult r = await src.Task;
        Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
        Assert.Equal(1, svc.DismissCallCount);
        Assert.Equal(id, svc.DismissedId);
    }

    [Fact]
    public async Task Already_cancelled_token_completes_immediately()
    {
        Guid id = Guid.NewGuid();
        FakeService svc = new();
        CancellationToken alreadyCancelled = new CancellationToken(canceled: true);
        NotificationCompletionSource src = new(id, svc, alreadyCancelled);

        NotificationCloseResult r = await src.Task;
        Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
    }

    [Fact]
    public async Task Multiple_complete_calls_keep_first_result()
    {
        Guid id = Guid.NewGuid();
        FakeService svc = new();
        NotificationCompletionSource src = new(id, svc, CancellationToken.None);

        src.Complete(NotificationCloseReason.Timeout);
        src.Complete(NotificationCloseReason.BodyClick);

        NotificationCloseResult r = await src.Task;
        Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
    }
}
```

The `FakeService` uses interface members that don't exist yet (`ShowAsync`, `ShowDialogAsync`). They are added in Task 4.1. **Order matters:** complete Task 4.1 first if you prefer, or temporarily comment those members out and re-enable after Task 4.1 lands.

To keep TDD clean, **defer running these tests until after Task 4.1** lands. Use them as the failing checkpoint that drives Task 3.2 + 4.1.

- [ ] **Step 2: Implement `NotificationCompletionSource`**

Create `src/Notification.Core/Internal/NotificationCompletionSource.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notification.Core.Internal
{
    /// <summary>
    /// Bridges a notification's lifecycle to a <see cref="TaskCompletionSource{TResult}"/>
    /// that resolves with a <see cref="NotificationCloseResult"/>. Registers cancellation
    /// to invoke <see cref="INotificationService.Dismiss"/> and complete with
    /// <see cref="NotificationCloseReason.Cancelled"/>.
    /// </summary>
    internal sealed class NotificationCompletionSource
    {
        private readonly TaskCompletionSource<NotificationCloseResult> _tcs =
            new TaskCompletionSource<NotificationCloseResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly Guid _id;
        private CancellationTokenRegistration _ctReg;

        public NotificationCompletionSource(Guid id, INotificationService service, CancellationToken ct)
        {
            _id = id;
            if (ct.CanBeCanceled)
            {
                _ctReg = ct.Register(() =>
                {
                    try { service.Dismiss(_id); } catch { }
                    _tcs.TrySetResult(new NotificationCloseResult
                    {
                        NotificationId = _id,
                        Reason = NotificationCloseReason.Cancelled
                    });
                });
            }
        }

        public Task<NotificationCloseResult> Task => _tcs.Task;

        public void Complete(NotificationCloseReason reason, string buttonId = null, int? buttonIndex = null)
        {
            _ctReg.Dispose();
            _tcs.TrySetResult(new NotificationCloseResult
            {
                NotificationId = _id,
                Reason = reason,
                ButtonId = buttonId,
                ButtonIndex = buttonIndex
            });
        }
    }
}
```

- [ ] **Step 3: Commit (without test run — pending 4.1)**

```bash
git add src/Notification.Core/Internal/NotificationCompletionSource.cs Tests/Notification.Core.Tests/NotificationCompletionSourceTests.cs
git commit -m "feat(core): internal NotificationCompletionSource (tests gated on Task 4.1)"
```

---

## Stage 4: `INotificationService` extension and convenience extensions

### Task 4.1: Add `ShowAsync` and `ShowDialogAsync` to `INotificationService`

**Files:**
- Modify: `src/Notification.Core/Interfaces/INotificationService.cs`

- [ ] **Step 1: Update the interface**

Replace contents of `src/Notification.Core/Interfaces/INotificationService.cs` with:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notification.Core
{
    /// <summary>
    /// Provides methods for displaying and managing notifications, including
    /// asynchronous overloads that complete with a structured close reason.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>Displays a notification based on the specified request.</summary>
        /// <param name="request">The notification request.</param>
        /// <returns>The unique identifier of the displayed notification.</returns>
        Guid Show(NotificationRequest request);

        /// <summary>Dismisses a specific notification by its identifier.</summary>
        void Dismiss(Guid notificationId);

        /// <summary>Dismisses all currently displayed notifications.</summary>
        void DismissAll();

        /// <summary>
        /// Displays a notification and returns a Task that completes when it closes,
        /// carrying the reason and (when applicable) the action button identifier.
        /// </summary>
        /// <param name="request">The notification request.</param>
        /// <param name="cancellationToken">When cancelled, dismisses the notification and completes the Task with <see cref="NotificationCloseReason.Cancelled"/>.</param>
        Task<NotificationCloseResult> ShowAsync(
            NotificationRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Displays a dialog notification requiring an explicit choice. The request must
        /// declare at least one button via <see cref="NotificationRequest.Buttons"/>. The
        /// returned Task completes when the user picks a button, the timer elapses, the
        /// X button is pressed, or the operation is cancelled.
        /// </summary>
        /// <param name="request">The notification request. Must have <c>Buttons.Count &gt;= 1</c>.</param>
        /// <param name="fallback">Strategy applied when the platform cannot render every declared button (e.g. MAUI Snackbar).</param>
        /// <param name="cancellationToken">When cancelled, dismisses the notification and completes the Task with <see cref="NotificationCloseReason.Cancelled"/>.</param>
        Task<NotificationCloseResult> ShowDialogAsync(
            NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default);
    }
}
```

- [ ] **Step 2: Build expecting compile errors in all platform managers**

Run: `dotnet build Notification.Wpf.sln`
Expected: BUILD FAIL — each of `NotificationManager`, `AvaloniaNotificationManager`, `MauiNotificationManager`, `ConsoleNotificationManager` lacks the new members. This is the failing state that drives Stage 6, 7, 8, 9.

- [ ] **Step 3: Add temporary `throw new NotImplementedException()` stubs to keep the rest of the solution building**

In each platform manager (WPF, Avalonia, MAUI, Console), add at the bottom of the class:

```csharp
        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException("Implemented in Stage 6+");

        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException("Implemented in Stage 6+");
```

Required `using` (add if missing): `using System.Threading;`, `using System.Threading.Tasks;`.

- [ ] **Step 4: Build again — solution should compile**

Run: `dotnet build Notification.Wpf.sln`
Expected: PASS (warnings allowed; the stubs throw at runtime, which is fine pending Stage 6+).

- [ ] **Step 5: Run Stage 3 deferred tests now**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationCompletionSourceTests"`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Notification.Core/Interfaces/INotificationService.cs \
        src/Notification.Wpf/Base/NotificationManager.cs \
        src/Notification.Avalonia/AvaloniaNotificationManager.cs \
        src/Notification.Maui/MauiNotificationManager.cs \
        src/Notification.Console/ConsoleNotificationManager.cs
git commit -m "feat(core)!: add ShowAsync/ShowDialogAsync to INotificationService (stub impls)"
```

---

### Task 4.2: Convenience `ShowAsync` extension methods

**Files:**
- Create: `src/Notification.Core/Extensions/NotificationServiceAsyncExtensions.cs`
- Create: `Tests/Notification.Core.Tests/NotificationServiceAsyncExtensionsTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Tests/Notification.Core.Tests/NotificationServiceAsyncExtensionsTests.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Notification.Core;
using Xunit;

namespace Notification.Core.Tests;

public class NotificationServiceAsyncExtensionsTests
{
    private sealed class CapturingService : INotificationService
    {
        public NotificationRequest LastRequest { get; private set; }
        public CancellationToken LastCt { get; private set; }
        public Guid Show(NotificationRequest request) => request.Id;
        public void Dismiss(Guid id) { }
        public void DismissAll() { }
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest req, CancellationToken ct = default)
        {
            LastRequest = req; LastCt = ct;
            return Task.FromResult(new NotificationCloseResult { NotificationId = req.Id, Reason = NotificationCloseReason.Timeout });
        }
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest req,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken ct = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Single_argument_overload_builds_message_only_request()
    {
        CapturingService svc = new();
        NotificationCloseResult r = await svc.ShowAsync("hello", NotificationType.Information);

        Assert.NotNull(svc.LastRequest);
        Assert.Null(svc.LastRequest.Title);
        Assert.Equal("hello", svc.LastRequest.Message);
        Assert.Equal(NotificationType.Information, svc.LastRequest.Type);
        Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
    }

    [Fact]
    public async Task Title_message_overload_sets_both()
    {
        CapturingService svc = new();
        await svc.ShowAsync("T", "M", NotificationType.Success);

        Assert.Equal("T", svc.LastRequest.Title);
        Assert.Equal("M", svc.LastRequest.Message);
        Assert.Equal(NotificationType.Success, svc.LastRequest.Type);
    }

    [Fact]
    public async Task Cancellation_token_is_passed_through()
    {
        CapturingService svc = new();
        CancellationTokenSource cts = new();
        await svc.ShowAsync("x", cancellationToken: cts.Token);
        Assert.Equal(cts.Token, svc.LastCt);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationServiceAsyncExtensionsTests"`
Expected: BUILD FAIL — extension class not found.

- [ ] **Step 3: Implement extensions**

Create `src/Notification.Core/Extensions/NotificationServiceAsyncExtensions.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notification.Core
{
    /// <summary>
    /// Convenience overloads for <see cref="INotificationService.ShowAsync"/>.
    /// </summary>
    public static class NotificationServiceAsyncExtensions
    {
        /// <summary>Show a message-only notification asynchronously.</summary>
        public static Task<NotificationCloseResult> ShowAsync(
            this INotificationService service,
            string message,
            NotificationType type = NotificationType.None,
            TimeSpan? expirationTime = null,
            string areaName = "",
            CancellationToken cancellationToken = default)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            return service.ShowAsync(new NotificationRequest
            {
                Message = message,
                Type = type,
                ExpirationTime = expirationTime,
                AreaName = areaName ?? ""
            }, cancellationToken);
        }

        /// <summary>Show a title + message notification asynchronously.</summary>
        public static Task<NotificationCloseResult> ShowAsync(
            this INotificationService service,
            string title, string message,
            NotificationType type = NotificationType.None,
            TimeSpan? expirationTime = null,
            string areaName = "",
            CancellationToken cancellationToken = default)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            return service.ShowAsync(new NotificationRequest
            {
                Title = title,
                Message = message,
                Type = type,
                ExpirationTime = expirationTime,
                AreaName = areaName ?? ""
            }, cancellationToken);
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "NotificationServiceAsyncExtensionsTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Extensions/NotificationServiceAsyncExtensions.cs Tests/Notification.Core.Tests/NotificationServiceAsyncExtensionsTests.cs
git commit -m "feat(core): add ShowAsync convenience extension overloads"
```

---

## Stage 5: `NotificationLifecycleEventArgs` enhancement

### Task 5.1: Add nullable `CloseReason` + new constructor

**Files:**
- Modify: `src/Notification.Core/Events/NotificationLifecycleEventArgs.cs`
- Modify: `Tests/Notification.Core.Tests/NotificationEventServiceTests.cs` (add new tests)

- [ ] **Step 1: Add failing test**

Append to `Tests/Notification.Core.Tests/NotificationEventServiceTests.cs`:

```csharp
[Fact]
public void Lifecycle_event_args_carries_close_reason_when_constructor_used()
{
    System.Guid id = System.Guid.NewGuid();
    NotificationLifecycleEventArgs e = new(id, NotificationLifecycleStage.Closed,
        "T", "M", NotificationCloseReason.ActionButton);

    Assert.Equal(NotificationCloseReason.ActionButton, e.CloseReason);
}

[Fact]
public void Lifecycle_event_args_close_reason_is_null_when_legacy_constructor_used()
{
    NotificationLifecycleEventArgs e = new(System.Guid.NewGuid(), NotificationLifecycleStage.Shown, "T", "M");
    Assert.Null(e.CloseReason);
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "Lifecycle_event_args"`
Expected: BUILD FAIL — `CloseReason` property and 5-arg constructor not found.

- [ ] **Step 3: Update `NotificationLifecycleEventArgs.cs`**

Replace contents of `src/Notification.Core/Events/NotificationLifecycleEventArgs.cs` with:

```csharp
using System;

namespace Notification.Core
{
    /// <summary>
    /// Provides event data for notification lifecycle events such as shown, clicked, closed, and dismissed.
    /// </summary>
    public sealed class NotificationLifecycleEventArgs : EventArgs
    {
        /// <summary>Gets the unique identifier of the notification that triggered this event.</summary>
        public Guid NotificationId { get; }

        /// <summary>Gets the lifecycle stage that the notification has entered.</summary>
        public NotificationLifecycleStage Stage { get; }

        /// <summary>Gets the UTC timestamp when this lifecycle event occurred.</summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>Gets the title of the notification at the time of the event.</summary>
        public string Title { get; }

        /// <summary>Gets the message of the notification at the time of the event.</summary>
        public string Message { get; }

        /// <summary>
        /// Gets why the notification was closed. Populated only for
        /// <see cref="NotificationLifecycleStage.Closed"/> and <see cref="NotificationLifecycleStage.Dismissed"/>.
        /// </summary>
        public NotificationCloseReason? CloseReason { get; }

        /// <summary>Initializes a lifecycle event without a close reason.</summary>
        public NotificationLifecycleEventArgs(
            Guid notificationId,
            NotificationLifecycleStage stage,
            string title,
            string message)
            : this(notificationId, stage, title, message, closeReason: null)
        {
        }

        /// <summary>Initializes a lifecycle event with a close reason.</summary>
        public NotificationLifecycleEventArgs(
            Guid notificationId,
            NotificationLifecycleStage stage,
            string title,
            string message,
            NotificationCloseReason? closeReason)
        {
            NotificationId = notificationId;
            Stage = stage;
            Timestamp = DateTimeOffset.UtcNow;
            Title = title;
            Message = message;
            CloseReason = closeReason;
        }
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj --filter "Lifecycle_event_args"`
Expected: PASS (2 new tests).

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Core/Events/NotificationLifecycleEventArgs.cs Tests/Notification.Core.Tests/NotificationEventServiceTests.cs
git commit -m "feat(core): NotificationLifecycleEventArgs carries optional CloseReason"
```

---

## Stage 6: WPF implementation

### Task 6.1: `NotificationClosedRoutedEventArgs`

**Files:**
- Create: `src/Notification.Wpf/Controls/Notifications/NotificationClosedRoutedEventArgs.cs`

- [ ] **Step 1: Implement the args type**

Create `src/Notification.Wpf/Controls/Notifications/NotificationClosedRoutedEventArgs.cs`:

```csharp
using System.Windows;
using Notification.Core;

namespace Notification.Wpf.Controls
{
    /// <summary>
    /// Routed event args raised by <see cref="Notification.NotificationClosed"/> carrying
    /// the close reason and the action button information (when applicable).
    /// </summary>
    public sealed class NotificationClosedRoutedEventArgs : RoutedEventArgs
    {
        /// <summary>Why the notification was closed.</summary>
        public NotificationCloseReason Reason { get; }

        /// <summary>The action button id when <see cref="Reason"/> is <see cref="NotificationCloseReason.ActionButton"/>, otherwise null.</summary>
        public string ButtonId { get; }

        /// <summary>The zero-based action button index when applicable.</summary>
        public int? ButtonIndex { get; }

        public NotificationClosedRoutedEventArgs(
            RoutedEvent routedEvent,
            object source,
            NotificationCloseReason reason,
            string buttonId,
            int? buttonIndex)
            : base(routedEvent, source)
        {
            Reason = reason;
            ButtonId = buttonId;
            ButtonIndex = buttonIndex;
        }
    }
}
```

- [ ] **Step 2: Build to verify no errors**

Run: `dotnet build src/Notification.Wpf/Notification.Wpf.csproj`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/Notification.Wpf/Controls/Notifications/NotificationClosedRoutedEventArgs.cs
git commit -m "feat(wpf): NotificationClosedRoutedEventArgs with reason/button info"
```

---

### Task 6.2: Internal close-hint fields on `Notification`

**Files:**
- Modify: `src/Notification.Wpf/Controls/Notifications/PART_CloseButton.cs` (the `Close()` method)
- Modify: `src/Notification.Wpf/Controls/Notifications/Notification.cs` (the partial class — add hint fields)

- [ ] **Step 1: Add hint fields to `Notification.cs`**

In `src/Notification.Wpf/Controls/Notifications/Notification.cs`, inside the `partial class Notification` body, add:

```csharp
        /// <summary>Internal hint set by area / close-button handlers before <see cref="Close"/> raises the routed event.</summary>
        internal Notification.Core.NotificationCloseReason? PendingCloseReason { get; set; }

        /// <summary>Internal hint — action button id when an action button caused the close.</summary>
        internal string PendingButtonId { get; set; }

        /// <summary>Internal hint — action button index when an action button caused the close.</summary>
        internal int? PendingButtonIndex { get; set; }
```

If the file does not already have `using Notification.Core;`, leave the fully qualified names as above.

- [ ] **Step 2: Update `Close()` to raise the enriched args**

In `src/Notification.Wpf/Controls/Notifications/PART_CloseButton.cs`, replace the `Close` method body's `RaiseEvent` lines (around lines 105–107) so it reads:

```csharp
            RaiseEvent(new RoutedEventArgs(NotificationCloseInvokedEvent));
            await Task.Delay(_closingAnimationTime);

            Notification.Core.NotificationCloseReason reason =
                PendingCloseReason ?? Notification.Core.NotificationCloseReason.Timeout;
            RaiseEvent(new NotificationClosedRoutedEventArgs(
                NotificationClosedEvent, this, reason, PendingButtonId, PendingButtonIndex));
```

Keep everything else in the method as is.

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/Notification.Wpf/Notification.Wpf.csproj`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add src/Notification.Wpf/Controls/Notifications/Notification.cs src/Notification.Wpf/Controls/Notifications/PART_CloseButton.cs
git commit -m "feat(wpf): Notification carries pending close reason/button hints into raised event"
```

---

### Task 6.3: Set close hints in `NotificationArea` paths

**Files:**
- Modify: `src/Notification.Wpf/Controls/NotificationArea.cs`

We need every path that ends in `notification.Close(...)` to set `PendingCloseReason` first:

- Body click (lines around 152–163): `PendingCloseReason = BodyClick`
- Expiration timer in `OnShowContent` (find where the timer triggers `notification.Close(_overlayWindow)`): `PendingCloseReason = Timeout`
- Max-items eviction (lines 252, 285): `PendingCloseReason = Cancelled` (programmatic eviction is effectively a dismiss)
- The X button is wired via `PART_CloseButton.OnCloseButtonOnClick` (`PART_CloseButton.cs:84`). It calls `Close()` without an overlay. Inject the hint there: before `Close();`, set the parent notification's `PendingCloseReason = XButton`.
- Left/Right action buttons live on the WPF `NotificationContent` template. Their click handlers wrap `LeftButtonAction` / `RightButtonAction`. Wrap them in `NotificationArea.Show(...)` so that the notification carries `PendingCloseReason = ActionButton` plus `PendingButtonId = "left"` (or `"right"`) and `PendingButtonIndex = 0/1` before the notification closes.

- [ ] **Step 1: Body click path**

In `OnMouseLeftButtonDown` (around lines 152–163), at the very start add:

```csharp
                Notification n = (Notification)sender;
                n.PendingCloseReason ??= Notification.Core.NotificationCloseReason.BodyClick;
```

- [ ] **Step 2: Expiration timer path**

Find the `Task.Delay`/`DispatcherTimer` path that triggers expiration close inside `OnShowContent` (look for `notification.Close(_overlayWindow)` near a `Task.Delay`). Immediately before the close call:

```csharp
            notification.PendingCloseReason ??= Notification.Core.NotificationCloseReason.Timeout;
```

- [ ] **Step 3: Eviction path**

For each eviction call site (lines ~252 and ~285), set:

```csharp
            evictedNotification.PendingCloseReason ??= Notification.Core.NotificationCloseReason.Cancelled;
```

(replace `evictedNotification` with the actual local variable name at that line).

- [ ] **Step 4: X button path**

In `src/Notification.Wpf/Controls/Notifications/PART_CloseButton.cs`, modify `OnCloseButtonOnClick`:

```csharp
        private void OnCloseButtonOnClick(object sender, RoutedEventArgs args)
        {
            if (sender is not Button button) return;

            button.Click -= OnCloseButtonOnClick;

            this.PendingCloseReason ??= Notification.Core.NotificationCloseReason.XButton;
            Close();
        }
```

- [ ] **Step 5: Action buttons (legacy 2-slot for this PR)**

In `NotificationArea.Show(object content, ...)` (around lines 146–186), after creating the `Notification` and before subscribing handlers, replace the section that currently lets the `NotificationContent` invoke `LeftButtonAction` / `RightButtonAction` raw with a wrapper that records the hint. The minimum-touch approach: in `NotificationArea.Show`, when `content is NotificationContent nc`:

```csharp
                if (content is NotificationContent nc)
                {
                    if (nc.LeftButtonAction is not null)
                    {
                        Action originalLeft = nc.LeftButtonAction;
                        nc.LeftButtonAction = () =>
                        {
                            notification.PendingCloseReason ??= Notification.Core.NotificationCloseReason.ActionButton;
                            notification.PendingButtonId ??= "left";
                            notification.PendingButtonIndex ??= 0;
                            originalLeft();
                        };
                    }

                    if (nc.RightButtonAction is not null)
                    {
                        Action originalRight = nc.RightButtonAction;
                        nc.RightButtonAction = () =>
                        {
                            notification.PendingCloseReason ??= Notification.Core.NotificationCloseReason.ActionButton;
                            notification.PendingButtonId ??= "right";
                            notification.PendingButtonIndex ??= 1;
                            originalRight();
                        };
                    }
                }
```

(If `NotificationManager.Show(NotificationRequest)` builds a `NotificationContent` from `Buttons[]` instead of legacy fields — added in Task 6.4 — these wrappers should pick the synthetic ids correctly because `Buttons[0].Id` / `Buttons[1].Id` are projected into `LeftButtonContent` / `RightButtonContent` and the action wrapper above writes the chosen synthetic id.)

For real `Buttons[]` with custom ids: in Task 6.4 below, the manager projects `Buttons[0]` to the Left slot and stores its real `Id` separately on the notification (`PendingButtonId` is overwritten on click with the real id). The wrapper here uses `??=` so any earlier value (set by the manager) survives.

- [ ] **Step 6: Build**

Run: `dotnet build src/Notification.Wpf/Notification.Wpf.csproj`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Notification.Wpf/Controls/NotificationArea.cs src/Notification.Wpf/Controls/Notifications/PART_CloseButton.cs
git commit -m "feat(wpf): set close-reason hints in body/X/timeout/eviction/action paths"
```

---

### Task 6.4: `NotificationManager.ShowAsync` (WPF)

**Files:**
- Modify: `src/Notification.Wpf/Base/NotificationManager.cs`
- Create: `Tests/Notification.Wpf.Tests/NotificationManagerAsyncTests.cs`

The behavioral tests use the existing `STAThread`-enabled fixture infrastructure in `Notification.Wpf.Tests` if present; otherwise pick the dispatcher pattern already used in that project.

- [ ] **Step 1: Inspect existing test fixtures**

Read `Tests/Notification.Wpf.Tests/*.csproj` and at least one existing test file to mirror the dispatcher / fixture pattern used. The new test file MUST follow the same approach. (This step is exploratory; if no WPF tests exist yet, fall back to a `[StaFact]` from `Xunit.StaFact` package — add the package to `Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj` if needed.)

- [ ] **Step 2: Write failing behavioral tests**

Create `Tests/Notification.Wpf.Tests/NotificationManagerAsyncTests.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Notification.Core;
using Notification.Wpf;
using Xunit;

namespace Notification.Wpf.Tests;

public class NotificationManagerAsyncTests
{
    private static NotificationManager NewManager() =>
        new NotificationManager(Dispatcher.CurrentDispatcher);

    [StaFact]
    public async Task ShowAsync_completes_with_timeout_when_expiration_elapses()
    {
        NotificationManager mgr = NewManager();
        Task<NotificationCloseResult> task = mgr.ShowAsync(new NotificationRequest
        {
            Message = "hi",
            ExpirationTime = TimeSpan.FromMilliseconds(100)
        });

        NotificationCloseResult r = await task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
    }

    [StaFact]
    public async Task ShowAsync_completes_with_cancelled_when_token_cancels()
    {
        NotificationManager mgr = NewManager();
        CancellationTokenSource cts = new();
        Task<NotificationCloseResult> task = mgr.ShowAsync(new NotificationRequest
        {
            Message = "hi",
            ExpirationTime = TimeSpan.FromSeconds(30)
        }, cts.Token);

        cts.Cancel();
        NotificationCloseResult r = await task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
    }

    [StaFact]
    public async Task ShowAsync_completes_with_cancelled_when_dismiss_called()
    {
        NotificationManager mgr = NewManager();
        NotificationRequest req = new() { Message = "x", ExpirationTime = TimeSpan.FromSeconds(30) };
        Task<NotificationCloseResult> task = mgr.ShowAsync(req);

        mgr.Dismiss(req.Id);
        NotificationCloseResult r = await task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
    }
}
```

- [ ] **Step 3: Run — expected to fail because `ShowAsync` currently throws `NotImplementedException`**

Run: `dotnet test Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj --filter "NotificationManagerAsyncTests"`
Expected: FAIL (NotImplementedException).

- [ ] **Step 4: Implement `ShowAsync` in `NotificationManager`**

In `src/Notification.Wpf/Base/NotificationManager.cs`, add field at the top of the class:

```csharp
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Notification.Core.Internal.NotificationCompletionSource> _completions = new();
```

Replace the `ShowAsync` stub with:

```csharp
        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Validation: explicit Buttons + legacy fields → ambiguous.
#pragma warning disable CS0618
            bool hasLegacy = !string.IsNullOrEmpty(request.LeftButtonContent) || !string.IsNullOrEmpty(request.RightButtonContent);
#pragma warning restore CS0618
            if (request.Buttons is { Count: > 0 } && hasLegacy)
                throw new ArgumentException("NotificationRequest specifies both Buttons and legacy LeftButton*/RightButton* fields.", nameof(request));

            // Duplicate id check inside Buttons[].
            if (request.Buttons is { Count: > 1 })
            {
                System.Collections.Generic.HashSet<string> seen = new();
                foreach (NotificationButton b in request.Buttons)
                    if (!seen.Add(b.Id))
                        throw new ArgumentException($"Duplicate button id '{b.Id}' in NotificationRequest.Buttons.", nameof(request));
            }

            Notification.Core.Internal.NotificationCompletionSource src = new(request.Id, this, cancellationToken);
            _completions[request.Id] = src;

            // Project Buttons[0..1] into the legacy 2-slot rendering until full N-button XAML lands.
            // The chosen button id is captured into PendingButtonId via a wrapper installed in NotificationArea.
            ProjectButtonsOntoLegacySlots(request);

            // Wrap OnClose so we can complete the source with the hints carried on the WPF Notification.
            Action originalOnClose = request.OnClose;
            System.Collections.Generic.List<Notification.Wpf.Controls.Notification> created = new();
            request.OnClose = () =>
            {
                try { originalOnClose?.Invoke(); } catch { }
                Notification.Wpf.Controls.Notification n = created.Count > 0 ? created[0] : null;
                NotificationCloseReason reason = n?.PendingCloseReason ?? NotificationCloseReason.Timeout;
                src.Complete(reason, n?.PendingButtonId, n?.PendingButtonIndex);
                _completions.TryRemove(request.Id, out _);
            };

            // Reuse existing pipeline via Show(request), but capture the created Notification(s) through the
            // existing onCreated callback list. The existing Show(NotificationRequest) already records to
            // `created` via the internal `created.Add` capture (see Base/NotificationManager.cs:537–539).
            // To not duplicate that, we inline-call the dispatch path the same way that overload does:
            // build the same content + call ShowContent with onCreated = created.Add.
            // For Stage 1 implementation, simplest: call Show(request) and rely on its dismiss-registration
            // dictionary to expose the live Notification instances. If that is impractical, refactor
            // Show(NotificationRequest) to extract an internal helper.

            Guid id = Show(request);
            return src.Task;
        }

        private static void ProjectButtonsOntoLegacySlots(NotificationRequest request)
        {
            System.Collections.Generic.IReadOnlyList<NotificationButton> resolved =
                Notification.Core.Internal.NotificationButtonResolver.ResolveButtons(request);

            if (resolved.Count == 0) return;

#pragma warning disable CS0618
            if (string.IsNullOrEmpty(request.LeftButtonContent) && resolved.Count >= 1)
            {
                request.LeftButtonContent = resolved[0].Content;
                Action prev = request.LeftButtonAction;
                request.LeftButtonAction = prev;
            }

            if (string.IsNullOrEmpty(request.RightButtonContent) && resolved.Count >= 2)
            {
                request.RightButtonContent = resolved[1].Content;
                Action prev = request.RightButtonAction;
                request.RightButtonAction = prev;
            }
#pragma warning restore CS0618

            if (resolved.Count > 2)
                System.Diagnostics.Debug.WriteLine(
                    $"[Notification.Wpf] Dropping {resolved.Count - 2} button(s) beyond the legacy 2-slot UI; full N-button rendering is a follow-up.");
        }
```

**Note on the action-button id capture:** the `NotificationArea` wrapper (Task 6.3.5) sets `PendingButtonId = "left"` / `"right"`. To carry the **real** `Buttons[i].Id` instead of the synthetic slot id, extend `ProjectButtonsOntoLegacySlots` to also stash the resolved button into a small side dictionary keyed by `request.Id`, and have the manager substitute `n.PendingButtonId` with the real id in the `onClose` wrapper before calling `src.Complete`. This is done in the next sub-step.

- [ ] **Step 5: Real button id substitution**

Add field to the manager:

```csharp
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, System.Collections.Generic.IReadOnlyList<NotificationButton>> _buttonsById = new();
```

In `ShowAsync`, after `ProjectButtonsOntoLegacySlots(request);`, add:

```csharp
            System.Collections.Generic.IReadOnlyList<NotificationButton> resolvedButtons =
                Notification.Core.Internal.NotificationButtonResolver.ResolveButtons(request);
            if (resolvedButtons.Count > 0)
                _buttonsById[request.Id] = resolvedButtons;
```

In the `onClose` wrapper, replace the `src.Complete(...)` call with:

```csharp
                string realButtonId = n?.PendingButtonId;
                int? realIndex = n?.PendingButtonIndex;
                if (realIndex is int idx
                    && _buttonsById.TryGetValue(request.Id, out var realBtns)
                    && idx < realBtns.Count)
                {
                    realButtonId = realBtns[idx].Id;
                }
                src.Complete(reason, realButtonId, realIndex);
                _buttonsById.TryRemove(request.Id, out _);
                _completions.TryRemove(request.Id, out _);
```

- [ ] **Step 6: Run tests**

Run: `dotnet test Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj --filter "NotificationManagerAsyncTests"`
Expected: PASS (3 tests).

- [ ] **Step 7: Commit**

```bash
git add src/Notification.Wpf/Base/NotificationManager.cs Tests/Notification.Wpf.Tests/NotificationManagerAsyncTests.cs Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj
git commit -m "feat(wpf): implement INotificationService.ShowAsync"
```

---

### Task 6.5: `NotificationManager.ShowDialogAsync` (WPF)

**Files:**
- Modify: `src/Notification.Wpf/Base/NotificationManager.cs`
- Modify: `Tests/Notification.Wpf.Tests/NotificationManagerAsyncTests.cs`

- [ ] **Step 1: Add failing tests**

Append to `NotificationManagerAsyncTests.cs`:

```csharp
    [StaFact]
    public async Task ShowDialogAsync_throws_when_no_buttons()
    {
        NotificationManager mgr = NewManager();
        await Assert.ThrowsAsync<ArgumentException>(() => mgr.ShowDialogAsync(new NotificationRequest
        {
            Message = "x"
        }));
    }

    [StaFact]
    public async Task ShowDialogAsync_completes_with_timeout_when_no_user_interaction()
    {
        NotificationManager mgr = NewManager();
        NotificationRequest req = new()
        {
            Message = "x",
            ExpirationTime = TimeSpan.FromMilliseconds(100),
            Buttons = new[] { new NotificationButton { Id = "ok", Content = "OK", Role = NotificationButtonRole.Affirmative } }
        };
        NotificationCloseResult r = await mgr.ShowDialogAsync(req).WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
    }
```

- [ ] **Step 2: Run — expected to fail (NotImplementedException)**

Run: `dotnet test Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj --filter "ShowDialogAsync_"`
Expected: FAIL.

- [ ] **Step 3: Implement `ShowDialogAsync`**

In `src/Notification.Wpf/Base/NotificationManager.cs`, replace the stub:

```csharp
        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Buttons == null || request.Buttons.Count == 0)
                throw new ArgumentException("ShowDialogAsync requires NotificationRequest.Buttons to have at least one button.", nameof(request));

            // Dialog defaults
            request.ExpirationTime ??= TimeSpan.MaxValue;
            // CloseOnClick already defaults to true in NotificationRequest; opt-out for dialogs.
            request.CloseOnClick = false;

            // WPF renders at most two buttons via legacy slots in this PR.
            // Fallback handling for 3+ buttons:
            if (request.Buttons.Count > 2)
            {
                switch (fallback)
                {
                    case NotificationDialogFallback.Throw:
                        throw new NotSupportedException("WPF currently renders at most 2 buttons; full N-button rendering is a follow-up. Pass NotificationDialogFallback.ShowFirstAffirmative or SkipSilently.");
                    case NotificationDialogFallback.SkipSilently:
                        return Task.FromResult(new NotificationCloseResult
                        {
                            NotificationId = request.Id,
                            Reason = NotificationCloseReason.Timeout
                        });
                    case NotificationDialogFallback.ShowFirstAffirmative:
                    default:
                        NotificationButton picked = null;
                        foreach (NotificationButton b in request.Buttons)
                            if (b.Role == NotificationButtonRole.Affirmative) { picked = b; break; }
                        if (picked is null) picked = request.Buttons[0];
                        request.Buttons = new[] { picked };
                        break;
                }
            }

            return ShowAsync(request, cancellationToken);
        }
```

- [ ] **Step 4: Run tests**

Run: `dotnet test Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj --filter "ShowDialogAsync_"`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Wpf/Base/NotificationManager.cs Tests/Notification.Wpf.Tests/NotificationManagerAsyncTests.cs
git commit -m "feat(wpf): implement INotificationService.ShowDialogAsync"
```

---

## Stage 7: Avalonia implementation

### Task 7.1: Inspect Avalonia host close paths

**Files:** read `src/Notification.Avalonia/Controls/AvaloniaNotificationHost.cs` (path verified by `Glob src/Notification.Avalonia/Controls/*.cs`), `src/Notification.Avalonia/Progress/AvaloniaNotifierProgress.cs`, and `src/Notification.Avalonia/AvaloniaNotificationManager.cs`.

- [ ] **Step 1: Read the existing host close lifecycle**

Find every path that closes a toast (click, X button, timer). Identify the single point where the toast removal happens — that's the hook for setting `NotificationCloseReason`.

- [ ] **Step 2: Decide hook strategy**

Two options:
- **A.** Add internal `PendingCloseReason` (and button hint fields) on the toast control, same pattern as WPF.
- **B.** Wire a callback from each close path back to the manager's completion dictionary.

Default to **A** — it mirrors WPF and keeps the manager simple. If the toast control in Avalonia is generated dynamically (not a class), introduce a small `ToastCloseInfo` attached value.

- [ ] **Step 3: Commit (notes only — no code yet)**

Stage 7.1 produces no code change; record findings as a comment in the next task. Skip commit.

---

### Task 7.2: Implement `ShowAsync` in Avalonia

**Files:**
- Modify: `src/Notification.Avalonia/AvaloniaNotificationManager.cs`
- Modify: `src/Notification.Avalonia/Controls/AvaloniaNotificationHost.cs` (toast close-reason propagation)

- [ ] **Step 1: Write failing test (if Avalonia test project exists)**

Look for `Tests/Notification.Avalonia.Tests` — if absent, create `Tests/Notification.Avalonia.Tests/Notification.Avalonia.Tests.csproj` mirroring the structure of `Notification.Wpf.Tests` and adding `<ProjectReference Include="..\..\src\Notification.Avalonia\Notification.Avalonia.csproj" />`. Mirror the WPF test cases (timeout / cancellation / dismiss) using Avalonia's `HeadlessUnitTestSession` (Avalonia.Headless package — add `PackageReference Include="Avalonia.Headless.XUnit"`).

If integration testing is too heavy, use a unit-level test that calls `manager.ShowAsync` with `host == null` and asserts immediate `Timeout` completion (see Step 3 below).

- [ ] **Step 2: Add the close-reason hint to the toast lifecycle**

Apply Task 7.1 Option A: add internal `PendingCloseReason` (and pending button id/index) fields on the Avalonia toast control. Wire each existing close path (timeout timer, click handler, X button, `host.Close(id)`) to set the appropriate reason before removal.

- [ ] **Step 3: Implement `ShowAsync`**

In `src/Notification.Avalonia/AvaloniaNotificationManager.cs`, replace the stub:

```csharp
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Notification.Core.Internal.NotificationCompletionSource> _completions = new();

        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Same validation as WPF.
#pragma warning disable CS0618
            bool hasLegacy = !string.IsNullOrEmpty(request.LeftButtonContent) || !string.IsNullOrEmpty(request.RightButtonContent);
#pragma warning restore CS0618
            if (request.Buttons is { Count: > 0 } && hasLegacy)
                throw new ArgumentException("NotificationRequest specifies both Buttons and legacy LeftButton*/RightButton* fields.", nameof(request));

            if (request.Buttons is { Count: > 1 })
            {
                System.Collections.Generic.HashSet<string> seen = new();
                foreach (NotificationButton b in request.Buttons)
                    if (!seen.Add(b.Id))
                        throw new ArgumentException($"Duplicate button id '{b.Id}'.", nameof(request));
            }

            Notification.Core.Internal.NotificationCompletionSource src = new(request.Id, this, cancellationToken);
            _completions[request.Id] = src;

            AvaloniaNotificationHost host = ActiveHost;
            if (host == null)
            {
                src.Complete(NotificationCloseReason.Timeout);
                return src.Task;
            }

            // Reuse Show(request) which already calls host.ShowToast(...). Then subscribe to the
            // toast's close lifecycle to read PendingCloseReason and call src.Complete.
            host.RegisterCompletion(request.Id, (reason, buttonId, buttonIndex) =>
            {
                src.Complete(reason, buttonId, buttonIndex);
                _completions.TryRemove(request.Id, out _);
            });

            Show(request);
            return src.Task;
        }
```

`RegisterCompletion` is a new method on `AvaloniaNotificationHost`. Add to that class:

```csharp
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Action<Notification.Core.NotificationCloseReason, string, int?>> _completions = new();

        internal void RegisterCompletion(Guid id, Action<Notification.Core.NotificationCloseReason, string, int?> callback)
            => _completions[id] = callback;

        // In each existing close path (timeout, click, X, host.Close(id)) call:
        // if (_completions.TryRemove(id, out var cb)) cb(reason, buttonId, buttonIndex);
```

- [ ] **Step 4: Build solution and run tests**

Run: `dotnet build Notification.Wpf.sln` then `dotnet test Tests/Notification.Avalonia.Tests/Notification.Avalonia.Tests.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Avalonia/AvaloniaNotificationManager.cs src/Notification.Avalonia/Controls/AvaloniaNotificationHost.cs Tests/Notification.Avalonia.Tests/
git commit -m "feat(avalonia): implement INotificationService.ShowAsync"
```

---

### Task 7.3: `ShowDialogAsync` in Avalonia

**Files:**
- Modify: `src/Notification.Avalonia/AvaloniaNotificationManager.cs`

Mirror Task 6.5 WPF logic exactly: validate `Buttons` non-empty, set dialog defaults, apply `NotificationDialogFallback` for `>2` buttons (Avalonia same limitation in this PR), delegate to `ShowAsync`.

- [ ] **Step 1: Add failing test (in `Tests/Notification.Avalonia.Tests/AvaloniaNotificationManagerAsyncTests.cs`)** mirroring the two `ShowDialogAsync_` tests from Task 6.5 Step 1.

- [ ] **Step 2: Run, expected FAIL.**

- [ ] **Step 3: Implement `ShowDialogAsync`** — copy of Task 6.5 Step 3 implementation (the logic is identical; only the host name differs).

- [ ] **Step 4: Run, expected PASS.**

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Avalonia/AvaloniaNotificationManager.cs Tests/Notification.Avalonia.Tests/AvaloniaNotificationManagerAsyncTests.cs
git commit -m "feat(avalonia): implement INotificationService.ShowDialogAsync"
```

---

## Stage 8: MAUI implementation

### Task 8.1: `ShowAsync` in MAUI

**Files:**
- Modify: `src/Notification.Maui/MauiNotificationManager.cs`

- [ ] **Step 1: Add failing test**

If `Tests/Notification.Maui.Tests` does not exist, document that MAUI tests are skipped in CI but covered by manual sample-app verification at release time (§11.4 of the spec). Add a smoke check via the manager-level `ShowAsync(...)` returning a completed Task on the fallback platform branch.

In `Tests/Notification.Console.Tests` (or a new `Tests/Notification.Maui.Tests` project if you can stand up the MAUI workloads in CI), add a unit-level test that creates `MauiNotificationManager` on the desktop fallback branch (`#else` in the manager) and asserts the Task completes with `Timeout`.

- [ ] **Step 2: Implement `ShowAsync`**

In `src/Notification.Maui/MauiNotificationManager.cs`, replace the stub. The current `ShowSnackbar` already handles CT cancellation and click/timeout. Extend it to:

1. Capture whether the action button was clicked via a flag.
2. After `await snackbar.Show(cancellationToken)`, derive the reason and the chosen button id.
3. Resolve `request.Buttons` via `NotificationButtonResolver.ResolveButtons` and pick the first button's id when clicked.

Replace `Show(NotificationRequest)` body to optionally accept a completion source:

```csharp
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Notification.Core.Internal.NotificationCompletionSource> _completions = new();

        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

#pragma warning disable CS0618
            bool hasLegacy = !string.IsNullOrEmpty(request.LeftButtonContent) || !string.IsNullOrEmpty(request.RightButtonContent);
#pragma warning restore CS0618
            if (request.Buttons is { Count: > 0 } && hasLegacy)
                throw new ArgumentException("NotificationRequest specifies both Buttons and legacy LeftButton*/RightButton* fields.", nameof(request));

            Notification.Core.Internal.NotificationCompletionSource src = new(request.Id, this, cancellationToken);
            _completions[request.Id] = src;
            System.Collections.Generic.IReadOnlyList<NotificationButton> resolvedButtons =
                Notification.Core.Internal.NotificationButtonResolver.ResolveButtons(request);

            // Existing Show logic, but we replace the action click invocation to also call src.Complete.
            // The simplest path: capture into a CTS, then await ShowSnackbar separately so we can derive
            // the reason after it returns. The existing `ShowSnackbar` is internal; instead of duplicating
            // it, refactor it slightly to accept a `completion` callback.

            // ... refactor existing `Show(NotificationRequest)` to delegate to a private helper that
            // returns `Task` and reports back via a callback (reason, buttonId, buttonIndex). Then:

            _ = ShowSnackbarInternalAsync(request, resolvedButtons, src);
            return src.Task;
        }

        private async Task ShowSnackbarInternalAsync(
            NotificationRequest request,
            System.Collections.Generic.IReadOnlyList<NotificationButton> buttons,
            Notification.Core.Internal.NotificationCompletionSource src)
        {
            // ... move/adapt the existing #if ANDROID || IOS || MACCATALYST || WINDOWS body here.
            // On click: src.Complete(NotificationCloseReason.ActionButton, buttons.Count > 0 ? buttons[0].Id : null, 0); return.
            // On timeout: src.Complete(NotificationCloseReason.Timeout);
            // On cancellation: handled by the CT registration inside NotificationCompletionSource.
            // Always remove from _completions at the end.
            await Task.CompletedTask; // placeholder — implement by adapting current ShowSnackbar.
        }
```

(Full implementation: literally inline-adapt the existing `#if ANDROID || IOS || MACCATALYST || WINDOWS` block from current lines 80–130 into `ShowSnackbarInternalAsync`, replacing the existing `request.OnClose?.Invoke()` exit with `src.Complete(...)`. The current `Show(NotificationRequest)` sync method should remain as a thin caller that does not await the task and ignores the completion source for backwards compatibility.)

- [ ] **Step 3: Build the MAUI project (if MAUI workloads are installed; otherwise document the skip)**

Run: `dotnet build src/Notification.Maui/Notification.Maui.csproj`
Expected: PASS if MAUI workloads installed; otherwise SKIP and document in commit message.

- [ ] **Step 4: Commit**

```bash
git add src/Notification.Maui/MauiNotificationManager.cs
git commit -m "feat(maui): implement INotificationService.ShowAsync"
```

---

### Task 8.2: `ShowDialogAsync` in MAUI

**Files:**
- Modify: `src/Notification.Maui/MauiNotificationManager.cs`

Snackbar supports exactly one action button. Implement the fallback rules per spec §5.3.2.

- [ ] **Step 1: Add failing test** mirroring the dialog tests (timeout completes, `ArgumentException` for empty Buttons, and one test for each `NotificationDialogFallback` value when `Buttons.Count == 3`).

- [ ] **Step 2: Run — FAIL.**

- [ ] **Step 3: Implement `ShowDialogAsync`**

```csharp
        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Buttons == null || request.Buttons.Count == 0)
                throw new ArgumentException("ShowDialogAsync requires Buttons.Count >= 1.", nameof(request));

            request.ExpirationTime ??= TimeSpan.MaxValue;
            request.CloseOnClick = false;

            if (request.Buttons.Count > 1)
            {
                switch (fallback)
                {
                    case NotificationDialogFallback.Throw:
                        throw new NotSupportedException("MAUI Snackbar supports at most one action button.");
                    case NotificationDialogFallback.SkipSilently:
                        return Task.FromResult(new NotificationCloseResult { NotificationId = request.Id, Reason = NotificationCloseReason.Timeout });
                    case NotificationDialogFallback.ShowFirstAffirmative:
                    default:
                        NotificationButton picked = null;
                        foreach (NotificationButton b in request.Buttons)
                            if (b.Role == NotificationButtonRole.Affirmative) { picked = b; break; }
                        if (picked is null) picked = request.Buttons[0];
                        request.Buttons = new[] { picked };
                        break;
                }
            }

            return ShowAsync(request, cancellationToken);
        }
```

- [ ] **Step 4: Run — PASS.**

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Maui/MauiNotificationManager.cs Tests/Notification.Maui.Tests/
git commit -m "feat(maui): implement INotificationService.ShowDialogAsync with Snackbar fallback"
```

---

## Stage 9: Console implementation

### Task 9.1: `ShowAsync` in Console

**Files:**
- Modify: `src/Notification.Console/ConsoleNotificationManager.cs`

- [ ] **Step 1: Failing test**

Create `Tests/Notification.Console.Tests/ConsoleNotificationManagerAsyncTests.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Notification.Console;
using Notification.Core;
using Xunit;

namespace Notification.Console.Tests;

public class ConsoleNotificationManagerAsyncTests
{
    [Fact]
    public async Task ShowAsync_completes_immediately_with_timeout()
    {
        ConsoleNotificationManager mgr = new();
        NotificationCloseResult r = await mgr.ShowAsync(new NotificationRequest { Message = "hi" });
        Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
    }

    [Fact]
    public async Task ShowAsync_returns_cancelled_when_token_already_cancelled()
    {
        ConsoleNotificationManager mgr = new();
        NotificationCloseResult r = await mgr.ShowAsync(new NotificationRequest { Message = "hi" }, new CancellationToken(true));
        Assert.Equal(NotificationCloseReason.Cancelled, r.Reason);
    }
}
```

- [ ] **Step 2: Run — FAIL (NotImplementedException).**

- [ ] **Step 3: Implement**

In `src/Notification.Console/ConsoleNotificationManager.cs`, replace stubs:

```csharp
        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(new NotificationCloseResult { NotificationId = request.Id, Reason = NotificationCloseReason.Cancelled });

            Show(request);
            return Task.FromResult(new NotificationCloseResult { NotificationId = request.Id, Reason = NotificationCloseReason.Timeout });
        }
```

- [ ] **Step 4: Run — PASS (2 tests).**

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Console/ConsoleNotificationManager.cs Tests/Notification.Console.Tests/ConsoleNotificationManagerAsyncTests.cs
git commit -m "feat(console): implement INotificationService.ShowAsync (non-interactive)"
```

---

### Task 9.2: `ShowDialogAsync` in Console

**Files:**
- Modify: `src/Notification.Console/ConsoleNotificationManager.cs`
- Modify: `Tests/Notification.Console.Tests/ConsoleNotificationManagerAsyncTests.cs`

- [ ] **Step 1: Failing tests**

Append:

```csharp
    [Fact]
    public async Task ShowDialogAsync_throws_when_buttons_empty()
    {
        ConsoleNotificationManager mgr = new();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            mgr.ShowDialogAsync(new NotificationRequest { Message = "x" }));
    }

    [Fact]
    public async Task ShowDialogAsync_ShowFirstAffirmative_returns_affirmative_button()
    {
        ConsoleNotificationManager mgr = new();
        NotificationRequest req = new()
        {
            Message = "Delete?",
            Buttons = new[]
            {
                new NotificationButton { Id = "delete", Content = "Delete", Role = NotificationButtonRole.Destructive },
                new NotificationButton { Id = "yes",    Content = "Yes",    Role = NotificationButtonRole.Affirmative },
                new NotificationButton { Id = "cancel", Content = "Cancel", Role = NotificationButtonRole.Negative }
            }
        };
        NotificationCloseResult r = await mgr.ShowDialogAsync(req);
        Assert.Equal(NotificationCloseReason.ActionButton, r.Reason);
        Assert.Equal("yes", r.ButtonId);
        Assert.Equal(1, r.ButtonIndex);
    }

    [Fact]
    public async Task ShowDialogAsync_SkipSilently_returns_timeout()
    {
        ConsoleNotificationManager mgr = new();
        NotificationRequest req = new()
        {
            Message = "x",
            Buttons = new[] { new NotificationButton { Id = "ok", Content = "OK" } }
        };
        NotificationCloseResult r = await mgr.ShowDialogAsync(req, NotificationDialogFallback.SkipSilently);
        Assert.Equal(NotificationCloseReason.Timeout, r.Reason);
    }

    [Fact]
    public async Task ShowDialogAsync_Throw_throws_NotSupported()
    {
        ConsoleNotificationManager mgr = new();
        NotificationRequest req = new()
        {
            Message = "x",
            Buttons = new[] { new NotificationButton { Id = "ok", Content = "OK" } }
        };
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            mgr.ShowDialogAsync(req, NotificationDialogFallback.Throw));
    }
```

- [ ] **Step 2: Run — FAIL.**

- [ ] **Step 3: Implement**

```csharp
        /// <inheritdoc />
        public Task<NotificationCloseResult> ShowDialogAsync(NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Buttons == null || request.Buttons.Count == 0)
                throw new ArgumentException("ShowDialogAsync requires Buttons.Count >= 1.", nameof(request));

            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(new NotificationCloseResult { NotificationId = request.Id, Reason = NotificationCloseReason.Cancelled });

            switch (fallback)
            {
                case NotificationDialogFallback.Throw:
                    throw new NotSupportedException("Interactive console dialogs are not supported; pass NotificationDialogFallback.ShowFirstAffirmative or SkipSilently.");

                case NotificationDialogFallback.SkipSilently:
                    Show(request);
                    return Task.FromResult(new NotificationCloseResult { NotificationId = request.Id, Reason = NotificationCloseReason.Timeout });

                case NotificationDialogFallback.ShowFirstAffirmative:
                default:
                    // Print the variants for visibility, then auto-pick.
                    Show(request);
                    System.Console.WriteLine($"[CHOICE] {string.Join(" | ", System.Linq.Enumerable.Select(request.Buttons, (b, i) => $"[{i}] {b.Content}"))}");

                    NotificationButton picked = null;
                    int pickedIdx = -1;
                    for (int i = 0; i < request.Buttons.Count; i++)
                        if (request.Buttons[i].Role == NotificationButtonRole.Affirmative) { picked = request.Buttons[i]; pickedIdx = i; break; }
                    if (picked is null) { picked = request.Buttons[0]; pickedIdx = 0; }

                    return Task.FromResult(new NotificationCloseResult
                    {
                        NotificationId = request.Id,
                        Reason = NotificationCloseReason.ActionButton,
                        ButtonId = picked.Id,
                        ButtonIndex = pickedIdx
                    });
            }
        }
```

- [ ] **Step 4: Run — PASS (4 new tests).**

- [ ] **Step 5: Commit**

```bash
git add src/Notification.Console/ConsoleNotificationManager.cs Tests/Notification.Console.Tests/ConsoleNotificationManagerAsyncTests.cs
git commit -m "feat(console): implement INotificationService.ShowDialogAsync (non-interactive fallback)"
```

---

## Stage 10: Documentation

### Task 10.1: README.md — Async API section

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add a top-level section after the existing intro**

Place near the top, before existing usage examples:

````markdown
## Async API (v11.0.0)

Awaitable overloads on `INotificationService` complete when a notification closes,
with a structured close reason.

```csharp
NotificationCloseResult r = await manager.ShowAsync("Saved", NotificationType.Success);
if (r.Reason == NotificationCloseReason.UserClick) DoNext();
```

For explicit dialogs use `ShowDialogAsync` with a `Buttons[]` collection:

```csharp
NotificationCloseResult r = await manager.ShowDialogAsync(new NotificationRequest
{
    Title = "Delete file?",
    Message = path,
    Type = NotificationType.Warning,
    Buttons = new[]
    {
        new NotificationButton { Id = "delete", Content = "Delete", Role = NotificationButtonRole.Destructive },
        new NotificationButton { Id = "cancel", Content = "Cancel", Role = NotificationButtonRole.Negative }
    }
}, cancellationToken: ct);

if (r.Reason == NotificationCloseReason.ActionButton && r.ButtonId == "delete")
    DeleteFile();
```

See `Migration.md` for upgrading from v10.
````

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs(readme): add Async API quick-start"
```

---

### Task 10.2: Documentation.md — full reference

**Files:**
- Modify: `Documentation.md`

- [ ] **Step 1: Append a new section "Async API"**

Sections to cover:
- `NotificationCloseReason` enum values and when each is raised on each platform.
- `NotificationCloseResult` fields with examples.
- `NotificationButton` and `NotificationButtonRole`.
- `ShowAsync(NotificationRequest, CancellationToken)` — semantics and per-platform notes.
- `ShowDialogAsync` — `NotificationDialogFallback` semantics with a per-platform behavior table.
- Convenience extensions (`ShowAsync(string, type, ...)`).

Use the spec sections §3.1–§3.8 as the source of truth. Provide one runnable C# snippet per public type.

- [ ] **Step 2: Commit**

```bash
git add Documentation.md
git commit -m "docs: add full Async API reference"
```

---

### Task 10.3: Migration.md — v10 → v11 migration

**Files:**
- Modify: `Migration.md`

- [ ] **Step 1: Append a "Migrating from v10.x to v11.0" section**

Cover, with before/after C# snippets:

1. **WPF `NotificationClosed` subscriber.**
   - Before: `void OnClosed(object s, RoutedEventArgs e) { /* reacted to fact of close */ }` — keeps compiling.
   - After: cast `e` to `NotificationClosedRoutedEventArgs` to read `e.Reason`, `e.ButtonId`, `e.ButtonIndex`.

2. **Legacy `LeftButtonAction`/`LeftButtonContent` → `Buttons[]`.**
   - Before:
     ```csharp
     req.LeftButtonContent = "OK";   req.LeftButtonAction  = onOk;
     req.RightButtonContent = "No";  req.RightButtonAction = onNo;
     ```
   - After:
     ```csharp
     req.Buttons = new[]
     {
         new NotificationButton { Id = "ok", Content = "OK", Role = NotificationButtonRole.Affirmative },
         new NotificationButton { Id = "no", Content = "No", Role = NotificationButtonRole.Negative }
     };
     ```

3. **`ShowButtonWindow` callback flow → `ShowDialogAsync`.**
   - Before: subscribe to the actions.
   - After: `await mgr.ShowDialogAsync(req)` and branch on `Reason`/`ButtonId`.

4. **External `INotificationService` implementers.**
   - Add `ShowAsync` and `ShowDialogAsync` methods. Use the `NotificationCompletionSource` pattern documented in `Documentation.md`. Provide a sample reference implementation snippet.

- [ ] **Step 2: Commit**

```bash
git add Migration.md
git commit -m "docs: add v10 → v11 migration guide for async API"
```

---

### Task 10.4: Updates.md + version bumps

**Files:**
- Modify: `Updates.md`
- Modify: `src/Notification.Wpf/Notification.Wpf.csproj` (`<Version>` → `11.0.0.0`)
- Modify: `src/Notification.Core/Notification.Core.csproj` (`<Version>` if present)
- Modify: `src/Notification.Avalonia/Notification.Avalonia.csproj` (`<Version>` if present)
- Modify: `src/Notification.Maui/Notification.Maui.csproj` (`<Version>` if present)
- Modify: `src/Notification.Console/Notification.Console.csproj` (`<Version>` if present)

- [ ] **Step 1: Add release-notes entry to `Updates.md`**

```markdown
## 11.0.0 — Async API with close reason

### New features
- `INotificationService.ShowAsync(NotificationRequest, CancellationToken)` — awaitable show that completes with a structured `NotificationCloseResult`.
- `INotificationService.ShowDialogAsync(NotificationRequest, NotificationDialogFallback, CancellationToken)` — explicit-choice dialog with N action buttons.
- `NotificationCloseReason`, `NotificationCloseResult`, `NotificationButton`, `NotificationButtonRole`, `NotificationDialogFallback` (`Notification.Core`).
- Convenience extensions `ShowAsync(string, type, ...)` and `ShowAsync(string, string, type, ...)`.
- WPF `Notification.NotificationClosed` now carries `NotificationClosedRoutedEventArgs` with `Reason` / `ButtonId` / `ButtonIndex`.
- `NotificationLifecycleEventArgs.CloseReason` (nullable) populated for Closed / Dismissed.

### Breaking changes
- WPF `NotificationClosed` event raises `NotificationClosedRoutedEventArgs : RoutedEventArgs`. Handlers typed as `(object, RoutedEventArgs)` continue to work; handlers casting `e` to a different args type break.
- `NotificationRequest.LeftButtonAction` / `LeftButtonContent` / `RightButtonAction` / `RightButtonContent` are marked `[Obsolete]`. They keep working in v11; removal is planned for v12.
- External implementations of `INotificationService` must add `ShowAsync` and `ShowDialogAsync`.

### Migration
See `Migration.md` for before/after code snippets and reference implementation patterns.

### Out of scope (planned follow-ups)
See `specs/roadmap.md`.
```

- [ ] **Step 2: Bump versions**

In each `.csproj` listed above, find `<Version>10.x.x.x</Version>` and change to `<Version>11.0.0.0</Version>`. If no `<Version>` is present in some projects (Core/Avalonia/Maui/Console), add one matching the WPF version.

- [ ] **Step 3: Build all**

Run: `dotnet build Notification.Wpf.sln`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add Updates.md src/Notification.Wpf/Notification.Wpf.csproj src/Notification.Core/Notification.Core.csproj src/Notification.Avalonia/Notification.Avalonia.csproj src/Notification.Maui/Notification.Maui.csproj src/Notification.Console/Notification.Console.csproj
git commit -m "chore(release): bump to 11.0.0 across all projects"
```

---

## Stage 11: Sample projects

### Task 11.1: WPF sample — Async API page

**Files:**
- Create: `Samples/Notification.Wpf.Sample/Views/AsyncApiView.xaml`
- Create: `Samples/Notification.Wpf.Sample/Views/AsyncApiView.xaml.cs`
- Modify: `Samples/Notification.Wpf.Sample/MainWindow.xaml` (add tab / navigation entry)

- [ ] **Step 1: Add a new view with three demo buttons:**
  1. `await mgr.ShowAsync("Saved", NotificationType.Success);` — write reason to a `TextBlock`.
  2. `await mgr.ShowDialogAsync(...)` with two buttons "Delete" / "Cancel" — show chosen `ButtonId`.
  3. `await mgr.ShowDialogAsync(...)` with three buttons + dropdown to pick `NotificationDialogFallback` — show how the fallback applies.
  4. CT demo: a Cancel button bound to a `CancellationTokenSource`, calling `cts.Cancel()` mid-show — show `Reason = Cancelled`.

- [ ] **Step 2: Verify the sample compiles**

Run: `dotnet build Samples/Notification.Wpf.Sample/Notification.Wpf.Sample.csproj`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add Samples/Notification.Wpf.Sample/Views/AsyncApiView.xaml Samples/Notification.Wpf.Sample/Views/AsyncApiView.xaml.cs Samples/Notification.Wpf.Sample/MainWindow.xaml
git commit -m "samples(wpf): add Async API demo page"
```

---

### Task 11.2: Avalonia sample — Async API page

**Files:**
- Create: `Samples/Notification.Avalonia.Sample/Views/AsyncApiView.axaml` and `.cs`
- Modify: `Samples/Notification.Avalonia.Sample/MainWindow.axaml`

Mirror Task 11.1 in Avalonia idioms (`UserControl` / `Window` with code-behind that calls `await _mgr.ShowAsync(...)`). Reuse the same three demo flows.

- [ ] **Step 1: Add the view.**
- [ ] **Step 2: Build.** Run `dotnet build Samples/Notification.Avalonia.Sample/Notification.Avalonia.Sample.csproj`. Expected PASS.
- [ ] **Step 3: Commit.**

```bash
git add Samples/Notification.Avalonia.Sample/
git commit -m "samples(avalonia): add Async API demo page"
```

---

### Task 11.3: MAUI sample — Async API page

**Files:**
- Create: `Samples/Notification.Maui.Sample/Pages/AsyncApiPage.xaml` and `.cs`
- Modify: `Samples/Notification.Maui.Sample/AppShell.xaml`

Demonstrate:
1. `await mgr.ShowAsync("Saved", Success)` — Snackbar without action.
2. `await mgr.ShowAsync(req with one button)` — Snackbar with single action, `ButtonId` returned.
3. `await mgr.ShowDialogAsync(req with 3 buttons, fallback)` — show that `ShowFirstAffirmative` collapses to one Snackbar action; `Throw` raises `NotSupportedException` (caught and displayed).

- [ ] **Step 1: Add the page.**
- [ ] **Step 2: Build.** Run `dotnet build Samples/Notification.Maui.Sample/Notification.Maui.Sample.csproj`. PASS if MAUI workloads installed; SKIP otherwise (document).
- [ ] **Step 3: Commit.**

```bash
git add Samples/Notification.Maui.Sample/
git commit -m "samples(maui): add Async API demo page with Snackbar fallback"
```

---

### Task 11.4: Console sample — Async CLI flow

**Files:**
- Modify: `Samples/Notification.Console.Sample/Program.cs`

Demonstrate sequential CLI flow:

```csharp
NotificationCloseResult r1 = await mgr.ShowAsync("Starting", NotificationType.Information);
NotificationCloseResult r2 = await mgr.ShowDialogAsync(new NotificationRequest
{
    Message = "Proceed?",
    Buttons = new[]
    {
        new NotificationButton { Id = "ok", Content = "OK", Role = NotificationButtonRole.Affirmative },
        new NotificationButton { Id = "no", Content = "No", Role = NotificationButtonRole.Negative }
    }
}, NotificationDialogFallback.ShowFirstAffirmative);
System.Console.WriteLine($"Picked: {r2.ButtonId}");
```

- [ ] **Step 1: Replace `Program.cs` content with the demo above (preserve existing demos as alternative commands).**
- [ ] **Step 2: Build.** Run `dotnet build Samples/Notification.Console.Sample/Notification.Console.Sample.csproj`. Expected PASS.
- [ ] **Step 3: Run the sample once to verify output.**

Run: `dotnet run --project Samples/Notification.Console.Sample/Notification.Console.Sample.csproj`
Expected: notification text printed; `Picked: ok` (the first Affirmative).

- [ ] **Step 4: Commit.**

```bash
git add Samples/Notification.Console.Sample/Program.cs
git commit -m "samples(console): add Async API CLI flow demo"
```

---

## Stage 12: Release gates

### Task 12.1: Full test run

- [ ] **Step 1: Build the solution from a clean state**

Run: `dotnet build Notification.Wpf.sln -c Release`
Expected: PASS with no errors and only the expected CS0618 warnings on internal legacy reads (suppressed via `#pragma` per implementation).

- [ ] **Step 2: Run the test suites**

Run:
```
dotnet test Tests/Notification.Core.Tests/Notification.Core.Tests.csproj -c Release
dotnet test Tests/Notification.Console.Tests/Notification.Console.Tests.csproj -c Release
dotnet test Tests/Notification.Wpf.Tests/Notification.Wpf.Tests.csproj -c Release
# Avalonia / MAUI tests only if their test projects exist and workloads are installed
```
Expected: all green.

- [ ] **Step 3: No code change → no commit. Document failures here if any.**

---

### Task 12.2: Create `specs/roadmap.md`

**Files:**
- Create: `specs/roadmap.md`

- [ ] **Step 1: Write the roadmap**

Create `specs/roadmap.md`:

```markdown
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
```

- [ ] **Step 2: Commit**

```bash
git add specs/roadmap.md
git commit -m "docs(roadmap): capture post-v11 follow-ups (RunWithProgressAsync, N-button UI, interactive Console)"
```

---

### Task 12.3: User sample-app confirmation gate

This task does not produce code. **Implementer (the agent) must explicitly ask the user to launch each sample project and exercise the new async APIs, then collect confirmation per platform.**

- [ ] **Step 1: Build the four samples in Release**

Run, in turn:
```
dotnet build Samples/Notification.Wpf.Sample/Notification.Wpf.Sample.csproj -c Release
dotnet build Samples/Notification.Avalonia.Sample/Notification.Avalonia.Sample.csproj -c Release
dotnet build Samples/Notification.Maui.Sample/Notification.Maui.Sample.csproj -c Release
dotnet build Samples/Notification.Console.Sample/Notification.Console.Sample.csproj -c Release
```
Expected: PASS.

- [ ] **Step 2: Ask the user to launch each sample and confirm**

Ask the user verbatim (in Russian, per global instructions):

> Запусти, пожалуйста, по очереди:
> 1. `Notification.Wpf.Sample` — открой страницу «Async API», проверь все 4 кнопки (ShowAsync, 2-button dialog, 3-button + fallback, CancellationToken).
> 2. `Notification.Avalonia.Sample` — то же самое.
> 3. `Notification.Maui.Sample` — то же самое (Snackbar single-action + 3-button с fallback).
> 4. `Notification.Console.Sample` — выполни `dotnet run`, проверь, что флоу проходит и пишется `Picked: ok`.
>
> Подтверди каждый из четырёх — без этого PR не релизим.

**Block on user confirmation.** Do not proceed to Task 12.4 until the user has confirmed all four samples.

- [ ] **Step 3: No commit (gate task).**

---

### Task 12.4: Tag v11.0.0

After user confirmation in Task 12.3.

- [ ] **Step 1: Final review of the diff against main**

Run: `git log --oneline dev..HEAD`
Expected: a series of commits matching the stages above.

- [ ] **Step 2: Tag**

```bash
git tag -a v11.0.0 -m "v11.0.0 — async ShowAsync/ShowDialogAsync with NotificationCloseReason"
```

- [ ] **Step 3: Push branch and tag (only when user explicitly authorizes the push)**

Ask the user:
> Можно ли пушить ветку `claude/async-show-close-reason-9a8660` и тег `v11.0.0` в origin?

Wait for explicit yes. Then:

```bash
git push origin claude/async-show-close-reason-9a8660
git push origin v11.0.0
```

- [ ] **Step 4: Open the PR**

```bash
gh pr create --base dev --title "feat: async ShowAsync/ShowDialogAsync with NotificationCloseReason (#86) — v11.0.0" --body "$(cat <<'EOF'
## Summary
- Adds `INotificationService.ShowAsync` / `ShowDialogAsync` returning `Task<NotificationCloseResult>` across WPF / Avalonia / MAUI / Console.
- Introduces `NotificationButton` collection on `NotificationRequest`; marks legacy `LeftButton*` / `RightButton*` `[Obsolete]`.
- Enriches WPF `NotificationClosed` with `NotificationClosedRoutedEventArgs` (Reason / ButtonId / ButtonIndex).
- Bumps versions to 11.0.0 across all packages.

Closes #86.

## Design and migration
- Spec: `specs/2026-06-08-async-show-with-close-reason-design.md`
- Roadmap: `specs/roadmap.md`
- Migration: `Migration.md` (v10 → v11 section)

## Test plan
- [x] All Core / Console / WPF unit and behavioral tests pass.
- [x] User confirmed each sample project (WPF / Avalonia / MAUI / Console) launched and exercised the new APIs end-to-end.
EOF
)"
```

- [ ] **Step 5: Mark plan complete.**

---

## Self-Review

### Spec coverage

| Spec section | Plan task |
|---|---|
| §3.1 `NotificationCloseReason` | 1.1 |
| §3.2 `NotificationCloseResult` | 1.2 |
| §3.3 `NotificationButton` / Role | 1.3 + 1.4 |
| §3.4 `NotificationDialogFallback` | 1.5 |
| §3.5 `NotificationRequest.Buttons` + `[Obsolete]` | 2.1 + 2.2 |
| §3.6 `INotificationService` extension | 4.1 |
| §3.7 Defaults differences | 6.5 (WPF), 7.3 (Avalonia), 8.2 (MAUI), 9.2 (Console) |
| §3.8 Convenience extensions | 4.2 |
| §3.9 `NotificationLifecycleEventArgs.CloseReason` | 5.1 |
| §4 `NotificationCompletionSource` | 3.2 |
| §5.1 WPF impl | 6.1–6.5 |
| §5.2 Avalonia impl | 7.1–7.3 |
| §5.3 MAUI impl | 8.1–8.2 |
| §5.4 Console impl | 9.1–9.2 |
| §6 `NotificationButtonResolver` | 3.1 |
| §7 Breaking changes | 4.1 + 6.1 + 2.2 + 5.1 |
| §8 Documentation | 10.1–10.4 |
| §9 Sample projects | 11.1–11.4 |
| §10 Tests | embedded in every TDD task |
| §11 Release checklist | 12.1–12.4 |
| §12.1 RunWithProgressAsync recommendation | 12.2 (`specs/roadmap.md`) |

All spec sections have a corresponding task.

### Placeholder scan

- No "TBD" / "TODO" / "later" left.
- Two tasks contain words like "adapt" or "mirror Task 6.5" (7.3, 8.2 fallback logic). In each case the surrounding step provides the exact implementation code from the source task in full, so the reader does not need to context-switch. Acceptable.

### Type / signature consistency

- `ShowAsync(NotificationRequest, CancellationToken)` — same signature across §4.1 (interface) and Stages 6, 7, 8, 9 (impls).
- `ShowDialogAsync(NotificationRequest, NotificationDialogFallback, CancellationToken)` — same across interface and impls.
- `NotificationCompletionSource.Complete(reason, buttonId, buttonIndex)` — same across Tasks 3.2 and the manager implementations.
- `NotificationButtonResolver.ResolveButtons(NotificationRequest)` — same across Tasks 3.1, 6.4, 8.1.
- WPF `Notification.PendingCloseReason` / `PendingButtonId` / `PendingButtonIndex` — same across 6.2, 6.3, 6.4.

Consistent.
