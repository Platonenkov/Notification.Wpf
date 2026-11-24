using System;
using System.Collections.Generic;
using System.Windows;

namespace Notification.Wpf.Base
{
    /// <summary>
    /// Tells the overlay when the host application has no window left. It never relies on
    /// <c>Application.MainWindow</c>: it subscribes to <c>Closed</c> of host windows and rescans on every close.
    /// UI thread only.
    /// </summary>
    internal sealed class HostWindowWatcher
    {
        private readonly Func<IReadOnlyList<Window>> _getHostWindows;
        private readonly Func<bool> _shouldReactToLastWindow;
        private readonly Action _onLastHostWindowClosed;
        private readonly List<Window> _tracked = new List<Window>();

        internal HostWindowWatcher(Func<IReadOnlyList<Window>> getHostWindows,
                                   Func<bool> shouldReactToLastWindow,
                                   Action onLastHostWindowClosed)
        {
            _getHostWindows = getHostWindows ?? throw new ArgumentNullException(nameof(getHostWindows));
            _shouldReactToLastWindow = shouldReactToLastWindow ?? throw new ArgumentNullException(nameof(shouldReactToLastWindow));
            _onLastHostWindowClosed = onLastHostWindowClosed ?? throw new ArgumentNullException(nameof(onLastHostWindowClosed));
        }

        /// <summary>Gets the number of host windows currently subscribed to.</summary>
        internal int TrackedCount => _tracked.Count;

        /// <summary>Subscribes to <c>Closed</c> of every host window that is not tracked yet.</summary>
        internal void Scan() => Scan(null);

        // The window that is closing is skipped: a collection may still list it while Closed runs, and
        // tracking it again would subscribe to an event that never fires a second time.
        private void Scan(Window closing)
        {
            foreach (Window window in _getHostWindows())
            {
                if (ReferenceEquals(window, closing) || _tracked.Contains(window))
                    continue;

                _tracked.Add(window);
                window.Closed += OnWindowClosed;
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Closed -= OnWindowClosed;
            _tracked.Remove(window);

            // Keeps the invariant: while any host window exists, at least one is tracked, so a window
            // opened after the last scan is picked up by the close of the one that was tracked.
            Scan(window);

            if (_tracked.Count == 0 && _shouldReactToLastWindow())
                _onLastHostWindowClosed();
        }
    }
}
