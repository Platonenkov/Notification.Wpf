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
        /// X button is pressed (where the platform exposes one), or the operation is cancelled.
        /// </summary>
        /// <param name="request">The notification request. Must have <c>Buttons.Count &gt;= 1</c>.</param>
        /// <param name="fallback">Strategy applied when the platform cannot render every declared button (e.g. WPF/Avalonia when more than two buttons are declared); platforms whose native surface renders all buttons — such as MAUI's action sheet — ignore this parameter.</param>
        /// <param name="cancellationToken">When cancelled, dismisses the notification and completes the Task with <see cref="NotificationCloseReason.Cancelled"/>.</param>
        Task<NotificationCloseResult> ShowDialogAsync(
            NotificationRequest request,
            NotificationDialogFallback fallback = NotificationDialogFallback.ShowFirstAffirmative,
            CancellationToken cancellationToken = default);
    }
}
