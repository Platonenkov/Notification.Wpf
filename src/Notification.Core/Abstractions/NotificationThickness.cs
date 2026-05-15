using System;

namespace Notification.Core
{
    public readonly struct NotificationThickness : IEquatable<NotificationThickness>
    {
        public double Left { get; }
        public double Top { get; }
        public double Right { get; }
        public double Bottom { get; }

        public NotificationThickness(double uniform) : this(uniform, uniform, uniform, uniform) { }

        public NotificationThickness(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public bool Equals(NotificationThickness other) =>
            Left.Equals(other.Left) && Top.Equals(other.Top) &&
            Right.Equals(other.Right) && Bottom.Equals(other.Bottom);

        public override bool Equals(object obj) => obj is NotificationThickness other && Equals(other);

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

        public static bool operator ==(NotificationThickness left, NotificationThickness right) => left.Equals(right);
        public static bool operator !=(NotificationThickness left, NotificationThickness right) => !left.Equals(right);
    }
}
