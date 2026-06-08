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
