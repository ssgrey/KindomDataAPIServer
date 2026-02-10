using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Tet.GeoSymbol
{
    public class ImageHelper
    {

        private static bool GetThumbmailImageCallback()
        {
            return false;
        }

        public static BitmapImage GetThumbnailImage(Image image, int size)
        {
            int srcWidth = image.Width;
            int srcHeight = image.Height;
            int srcMax = Math.Max(srcWidth, srcHeight);
            float ratio = size / (srcMax * 1.0f);
            float destWidth = srcWidth * ratio;
            float destHeight = srcHeight * ratio;
            Image destImage = image.GetThumbnailImage((int)destWidth, (int)destHeight, new Image.GetThumbnailImageAbort(GetThumbmailImageCallback), IntPtr.Zero);
            BitmapImage bitMapImage = ImageConverter.Convert(destImage);
            destImage.Dispose();
            destImage = null;
            return bitMapImage;
        }
    }
}
