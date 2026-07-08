using System;
using System.Collections.Generic;

namespace Notification.Core.Internal
{
    /// <summary>
    /// Shared validation for the asynchronous request surface, applied consistently by every
    /// platform implementation of <see cref="INotificationService.ShowAsync"/> and
    /// <see cref="INotificationService.ShowDialogAsync"/>.
    /// </summary>
    internal static class NotificationRequestValidator
    {
        /// <summary>
        /// Validates a request passed to the async API.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The request declares both <see cref="NotificationRequest.Buttons"/> and the obsolete
        /// legacy button fields, or a button is null, has an empty <see cref="NotificationButton.Id"/>,
        /// has a null <see cref="NotificationButton.Content"/>, or a duplicate id.
        /// </exception>
        public static void ValidateAsyncRequest(NotificationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

#pragma warning disable CS0618
            bool hasLegacy = !string.IsNullOrEmpty(request.LeftButtonContent)
                || !string.IsNullOrEmpty(request.RightButtonContent);
#pragma warning restore CS0618

            if (request.Buttons is { Count: > 0 } && hasLegacy)
                throw new ArgumentException(
                    "NotificationRequest specifies both Buttons and legacy LeftButton*/RightButton* fields.",
                    nameof(request));

            if (request.Buttons is { Count: > 0 })
            {
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (NotificationButton button in request.Buttons)
                {
                    if (button is null)
                        throw new ArgumentException("NotificationRequest.Buttons contains a null button.", nameof(request));
                    if (string.IsNullOrEmpty(button.Id))
                        throw new ArgumentException("NotificationButton.Id must be a non-empty value.", nameof(request));
                    if (button.Content is null)
                        throw new ArgumentException($"NotificationButton '{button.Id}' has a null Content.", nameof(request));
                    if (!seen.Add(button.Id))
                        throw new ArgumentException($"Duplicate button id '{button.Id}' in NotificationRequest.Buttons.", nameof(request));
                }
            }
        }
    }
}
