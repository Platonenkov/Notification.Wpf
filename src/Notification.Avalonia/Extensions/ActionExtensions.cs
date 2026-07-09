using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Notification.Avalonia.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Action"/> and asynchronous delegates executed on the UI thread.
/// </summary>
public static class ActionExtensions
{
    /// <summary>
    /// Executes the action immediately if already on the UI thread;
    /// otherwise posts it to the UI dispatcher.
    /// </summary>
    public static void InvokeOnUiThread(this Action action)
    {
        if (action is null)
            return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
    }

    /// <summary>
    /// Executes the asynchronous action on the UI thread.
    /// </summary>
    public static void InvokeOnUiThreadAsync(this Func<Task> action)
    {
        if (action is null)
            return;

        Dispatcher.UIThread.Post(async () =>
        {
            await action();
        });
    }
}
