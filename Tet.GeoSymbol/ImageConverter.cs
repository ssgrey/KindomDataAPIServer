using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tet.GeoSymbol
{
    public class ImageConverter
    {
        static public BitmapImage Convert(Image img)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Flush();
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
