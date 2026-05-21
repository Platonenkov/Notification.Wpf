using System.ComponentModel;
using System.Runtime.CompilerServices;
using Notifications.Wpf.Annotations;

namespace Notifications.Wpf.ViewModels.Base
{
    /// <summary>
    /// Base view model that implements <see cref="INotifyPropertyChanged"/> for data-bound classes.
    /// </summary>
    public abstract class ViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="PropertyName">The name of the property that changed. Automatically supplied by the caller.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));

        /// <summary>
        /// Sets the backing field to a new value and raises <see cref="PropertyChanged"/> if the value changed.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="field">A reference to the backing field of the property.</param>
        /// <param name="value">The new value to assign.</param>
        /// <param name="PropertyName">The name of the property. Automatically supplied by the caller.</param>
        /// <returns><c>true</c> if the value changed; otherwise, <c>false</c>.</returns>
        [NotifyPropertyChangedInvocator]
        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string PropertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(PropertyName);
            return true;
        }
    }
}
