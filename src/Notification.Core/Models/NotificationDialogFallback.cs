namespace Notification.Core
{
    /// <summary>
    /// Behavior when a platform cannot render all buttons of a dialog.
    /// </summary>
    /// <remarks>
    /// Honored by WPF and Avalonia (when more than two buttons are declared) and by Console.
    /// MAUI ignores this setting because its native action sheet renders any number of buttons.
    /// </remarks>
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
