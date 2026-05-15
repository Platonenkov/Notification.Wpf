using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Notification.Core;
using Notification.Wpf.Classes;

namespace Notification.Wpf.Adapters
{
    public static class ImageExtensions
    {
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
