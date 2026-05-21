using System;
using System.Globalization;

namespace Notification.Core
{
    /// <summary>
    /// Represents a platform-independent RGBA color for notification styling.
    /// </summary>
    public readonly struct NotificationColor : IEquatable<NotificationColor>
    {
        /// <summary>Gets the red component of the color (0–255).</summary>
        public byte R { get; }

        /// <summary>Gets the green component of the color (0–255).</summary>
        public byte G { get; }

        /// <summary>Gets the blue component of the color (0–255).</summary>
        public byte B { get; }

        /// <summary>Gets the alpha (opacity) component of the color (0–255).</summary>
        public byte A { get; }

        /// <summary>
        /// Initializes a new <see cref="NotificationColor"/> with the specified RGBA components.
        /// </summary>
        /// <param name="r">The red component (0–255).</param>
        /// <param name="g">The green component (0–255).</param>
        /// <param name="b">The blue component (0–255).</param>
        /// <param name="a">The alpha component (0–255). Defaults to 255 (fully opaque).</param>
        public NotificationColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Creates a fully opaque color from the specified RGB components.
        /// </summary>
        /// <param name="r">The red component (0–255).</param>
        /// <param name="g">The green component (0–255).</param>
        /// <param name="b">The blue component (0–255).</param>
        /// <returns>A new <see cref="NotificationColor"/> with alpha set to 255.</returns>
        public static NotificationColor FromRgb(byte r, byte g, byte b) => new NotificationColor(r, g, b);

        /// <summary>
        /// Creates a color from the specified ARGB components.
        /// </summary>
        /// <param name="a">The alpha component (0–255).</param>
        /// <param name="r">The red component (0–255).</param>
        /// <param name="g">The green component (0–255).</param>
        /// <param name="b">The blue component (0–255).</param>
        /// <returns>A new <see cref="NotificationColor"/> instance.</returns>
        public static NotificationColor FromArgb(byte a, byte r, byte g, byte b) => new NotificationColor(r, g, b, a);

        /// <summary>
        /// Parses a hexadecimal color string in #RRGGBB or #AARRGGBB format.
        /// </summary>
        /// <param name="hex">The hex color string, with or without a leading '#'.</param>
        /// <returns>A new <see cref="NotificationColor"/> parsed from the hex string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hex"/> is null or whitespace.</exception>
        /// <exception cref="FormatException">Thrown when the hex string is not in a valid format.</exception>
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

        /// <summary>
        /// Converts this color to its #AARRGGBB hexadecimal string representation.
        /// </summary>
        /// <returns>A hex string in the format #AARRGGBB.</returns>
        public string ToHex() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

        /// <summary>
        /// Implicitly converts a hex color string to a <see cref="NotificationColor"/>.
        /// </summary>
        /// <param name="hex">The hex color string to convert.</param>
        public static implicit operator NotificationColor(string hex) => FromHex(hex);

        /// <summary>Represents the color white (255, 255, 255).</summary>
        public static readonly NotificationColor White = new NotificationColor(255, 255, 255);

        /// <summary>Represents the color white smoke (245, 245, 245).</summary>
        public static readonly NotificationColor WhiteSmoke = new NotificationColor(245, 245, 245);

        /// <summary>Represents the color lime green (50, 205, 50).</summary>
        public static readonly NotificationColor LimeGreen = new NotificationColor(50, 205, 50);

        /// <summary>Represents the color orange (255, 165, 0).</summary>
        public static readonly NotificationColor Orange = new NotificationColor(255, 165, 0);

        /// <summary>Represents the color orange red (255, 69, 0).</summary>
        public static readonly NotificationColor OrangeRed = new NotificationColor(255, 69, 0);

        /// <summary>Represents the color cornflower blue (100, 149, 237).</summary>
        public static readonly NotificationColor CornflowerBlue = new NotificationColor(100, 149, 237);

        /// <summary>Represents the color dark gray (68, 68, 68).</summary>
        public static readonly NotificationColor DarkGray = new NotificationColor(68, 68, 68);

        /// <summary>Represents the color green (1, 211, 40).</summary>
        public static readonly NotificationColor Green = new NotificationColor(1, 211, 40);

        /// <inheritdoc />
        public bool Equals(NotificationColor other) => R == other.R && G == other.G && B == other.B && A == other.A;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is NotificationColor other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;

        /// <summary>Determines whether two <see cref="NotificationColor"/> instances are equal.</summary>
        public static bool operator ==(NotificationColor left, NotificationColor right) => left.Equals(right);

        /// <summary>Determines whether two <see cref="NotificationColor"/> instances are not equal.</summary>
        public static bool operator !=(NotificationColor left, NotificationColor right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => ToHex();
    }
}
