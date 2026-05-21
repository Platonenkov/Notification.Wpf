using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Notification.Core;
using Notification.Wpf.Classes;

namespace Notification.Wpf.Adapters
{
    /// <summary>
    /// Provides extension methods to convert image data between the framework-agnostic Core model
    /// (<see cref="NotificationImageData"/>) and the WPF model (<see cref="NotificationImage"/>).
    /// </summary>
    public static class ImageExtensions
    {
        /// <summary>
        /// Converts Core image data into a WPF <see cref="NotificationImage"/>, building an
        /// <see cref="ImageSource"/> from either a URI or raw byte data.
        /// </summary>
        /// <param name="data">The Core image data to convert; may be <see langword="null"/>.</param>
        /// <returns>
        /// A <see cref="NotificationImage"/> with a loaded image source and position,
        /// or <see langword="null"/> if <paramref name="data"/> is <see langword="null"/>.
        /// When raw data is used, the resulting bitmap is loaded into memory and frozen.
        /// </returns>
        public static NotificationImage ToWpfImage(this NotificationImageData data)
        {
            if (data == null)
                return null;

            ImageSource source = null;

            if (!string.IsNullOrEmpty(data.Uri))
            {
                source = new BitmapImage(new Uri(data.Uri, UriKind.RelativeOrAbsolute));
            }
            else if (data.RawData != null && data.RawData.Length > 0)
            {
                BitmapImage bitmap = new BitmapImage();
                using (MemoryStream stream = new MemoryStream(data.RawData))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                source = bitmap;
            }

            return new NotificationImage
            {
                Source = source,
                Position = data.Position
            };
        }

        /// <summary>
        /// Converts a WPF <see cref="NotificationImage"/> into framework-agnostic Core image data.
        /// </summary>
        /// <param name="image">The WPF image to convert; may be <see langword="null"/>.</param>
        /// <returns>
        /// A <see cref="NotificationImageData"/> instance carrying the image position,
        /// or <see langword="null"/> if <paramref name="image"/> is <see langword="null"/>.
        /// The WPF <see cref="ImageSource"/> itself is not transferred back to the Core model.
        /// </returns>
        public static NotificationImageData ToCoreImageData(this NotificationImage image)
        {
            if (image == null)
                return null;

            return new NotificationImageData
            {
                Position = image.Position
            };
        }
    }
}
