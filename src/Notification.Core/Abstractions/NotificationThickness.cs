using System;

namespace Notification.Core
{
    /// <summary>
    /// Represents a platform-independent thickness (margin or padding) with values for each side.
    /// </summary>
    public readonly struct NotificationThickness : IEquatable<NotificationThickness>
    {
        /// <summary>Gets the left side thickness in device-independent pixels.</summary>
        public double Left { get; }

        /// <summary>Gets the top side thickness in device-independent pixels.</summary>
        public double Top { get; }

        /// <summary>Gets the right side thickness in device-independent pixels.</summary>
        public double Right { get; }

        /// <summary>Gets the bottom side thickness in device-independent pixels.</summary>
        public double Bottom { get; }

        /// <summary>
        /// Initializes a new <see cref="NotificationThickness"/> with the same value applied to all four sides.
        /// </summary>
        /// <param name="uniform">The uniform thickness value for all sides.</param>
        public NotificationThickness(double uniform) : this(uniform, uniform, uniform, uniform) { }

        /// <summary>
        /// Initializes a new <see cref="NotificationThickness"/> with individual values for each side.
        /// </summary>
        /// <param name="left">The left side thickness.</param>
        /// <param name="top">The top side thickness.</param>
        /// <param name="right">The right side thickness.</param>
        /// <param name="bottom">The bottom side thickness.</param>
        public NotificationThickness(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <inheritdoc />
        public bool Equals(NotificationThickness other) =>
            Left.Equals(other.Left) && Top.Equals(other.Top) &&
            Right.Equals(other.Right) && Bottom.Equals(other.Bottom);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is NotificationThickness other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Left.GetHashCode();
                hash = hash * 31 + Top.GetHashCode();
                hash = hash * 31 + Right.GetHashCode();
                hash = hash * 31 + Bottom.GetHashCode();
                return hash;
            }
        }

        /// <summary>Determines whether two <see cref="NotificationThickness"/> instances are equal.</summary>
        public static bool operator ==(NotificationThickness left, NotificationThickness right) => left.Equals(right);

        /// <summary>Determines whether two <see cref="NotificationThickness"/> instances are not equal.</summary>
        public static bool operator !=(NotificationThickness left, NotificationThickness right) => !left.Equals(right);
    }
}
