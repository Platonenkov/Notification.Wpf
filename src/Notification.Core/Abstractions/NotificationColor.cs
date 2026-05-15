using System;
using System.Globalization;

namespace Notification.Core
{
    public readonly struct NotificationColor : IEquatable<NotificationColor>
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public NotificationColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static NotificationColor FromRgb(byte r, byte g, byte b) => new NotificationColor(r, g, b);

        public static NotificationColor FromArgb(byte a, byte r, byte g, byte b) => new NotificationColor(r, g, b, a);

        public static NotificationColor FromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentNullException(nameof(hex));

            hex = hex.TrimStart('#');

            switch (hex.Length)
            {
                case 6:
                    return new NotificationColor(
                        byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
                        byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                        byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber));
                case 8:
                    return new NotificationColor(
                        byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                        byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber),
                        byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber),
                        byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber));
                default:
                    throw new FormatException($"Invalid hex color format: #{hex}. Expected #RRGGBB or #AARRGGBB.");
            }
        }

        public string ToHex() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

        public static implicit operator NotificationColor(string hex) => FromHex(hex);

        public static readonly NotificationColor White = new NotificationColor(255, 255, 255);
        public static readonly NotificationColor WhiteSmoke = new NotificationColor(245, 245, 245);
        public static readonly NotificationColor LimeGreen = new NotificationColor(50, 205, 50);
        public static readonly NotificationColor Orange = new NotificationColor(255, 165, 0);
        public static readonly NotificationColor OrangeRed = new NotificationColor(255, 69, 0);
        public static readonly NotificationColor CornflowerBlue = new NotificationColor(100, 149, 237);
        public static readonly NotificationColor DarkGray = new NotificationColor(68, 68, 68);
        public static readonly NotificationColor Green = new NotificationColor(1, 211, 40);

        public bool Equals(NotificationColor other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object obj) => obj is NotificationColor other && Equals(other);
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;
        public static bool operator ==(NotificationColor left, NotificationColor right) => left.Equals(right);
        public static bool operator !=(NotificationColor left, NotificationColor right) => !left.Equals(right);
        public override string ToString() => ToHex();
    }
}
