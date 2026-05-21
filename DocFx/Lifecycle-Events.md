# Lifecycle Events

Subscribe to notification lifecycle events via `INotificationEventService`.

## Usage

```csharp
INotificationEventService events = provider.GetRequiredService<INotificationEventService>();

// All lifecycle changes
events.NotificationLifecycleChanged += (sender, e) =>
{
    Console.WriteLine($"[{e.Stage}] {e.Title} (ID: {e.NotificationId})");
};

// Specific events
events.NotificationShown += (s, e) => Log($"Shown: {e.Title}");
events.NotificationClicked += (s, e) => Log($"Clicked: {e.NotificationId}");
events.NotificationClosed += (s, e) => Log($"Closed: {e.Stage}");
```

## Lifecycle Stages

| Stage | Description |
|-------|-------------|
| `Shown` | Notification displayed on screen |
| `Clicked` | User clicked the notification body |
| `Closed` | Closed normally (by close button or programmatically) |
| `TimedOut` | Expired automatically after expiration time |
| `Dismissed` | Dismissed programmatically via `Dismiss()` |

## NotificationLifecycleEventArgs

| Property | Type | Description |
|----------|------|-------------|
| `NotificationId` | `Guid` | Unique notification identifier |
| `Stage` | `NotificationLifecycleStage` | Current lifecycle stage |
| `Timestamp` | `DateTimeOffset` | When the event occurred |
| `Title` | `string` | Notification title |
| `Message` | `string` | Notification message |

## Event Flow

```
Show() called
    |
    v
[Shown] ──── NotificationShown event
    |
    ├── User clicks ──── [Clicked] ──── NotificationClicked event
    |                        |
    |                        v
    |                    [Closed] ──── NotificationClosed event
    |
    ├── Timer expires ── [TimedOut] ── NotificationClosed event
    |
    └── Dismiss() ────── [Dismissed] ─ NotificationClosed event
```

## Tracking Active Notifications

```csharp
INotificationService service = /* from DI */;
INotificationEventService events = /* from DI */;

// Show returns the notification ID
Guid id = service.Show(NotificationBuilder
    .Create("Tracked", "This notification is tracked")
    .AsInformation()
    .Build());

// Dismiss by ID
service.Dismiss(id);

// Or dismiss all
service.DismissAll();
```
