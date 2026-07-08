using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Notification.Core.Internal
{
    /// <summary>
    /// Helpers for concurrent bookkeeping shared by the platform managers.
    /// </summary>
    internal static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// Atomically removes the entry for <paramref name="key"/> only when its current value is
        /// reference/value-equal to <paramref name="value"/>. Unlike
        /// <see cref="ConcurrentDictionary{TKey, TValue}.TryRemove(TKey, out TValue)"/> (which removes
        /// by key regardless of the current value), this prevents an in-flight entry from being torn
        /// out from under a concurrent operation that reused the same key. Implemented via the explicit
        /// <see cref="ICollection{T}"/> interface so it compiles on .NET Framework 4.8 and
        /// netstandard2.0, which lack the <c>TryRemove(KeyValuePair)</c> overload.
        /// </summary>
        /// <returns><see langword="true"/> when the matching entry was removed; otherwise <see langword="false"/>.</returns>
        public static bool TryRemovePair<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary)
                .Remove(new KeyValuePair<TKey, TValue>(key, value));
        }
    }
}
