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
