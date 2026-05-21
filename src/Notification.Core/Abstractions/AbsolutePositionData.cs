namespace Notification.Core
{
    /// <summary>
    /// Represents absolute position data for placing a notification at specific screen coordinates relative to a corner.
    /// </summary>
    public class AbsolutePositionData
    {
        /// <summary>Gets or sets the horizontal offset in pixels from the base corner.</summary>
        public double X { get; set; }

        /// <summary>Gets or sets the vertical offset in pixels from the base corner.</summary>
        public double Y { get; set; }

        /// <summary>Gets or sets the screen corner used as the origin for positioning.</summary>
        public Corner BaseCorner { get; set; }
    }
}
