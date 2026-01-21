using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace KindomDataAPIServer.Common
{
    public class ColorGenerator
    {
        private static readonly Color[] colors = new Color[]
        {
        Colors.Red,
        Colors.Green,
        Colors.Blue,
        Colors.Magenta,
        Colors.Orange,
        Colors.Yellow,
        Colors.Purple,
        Colors.Brown,
        Colors.Cyan,
        Colors.Gray,
        Colors.Pink,
        Colors.Lime,
        Colors.Turquoise,
        Colors.Violet,
        Colors.Gold,
        Colors.Silver,
        Colors.Teal,
        Colors.Navy,
        Colors.Olive,
        Colors.Maroon,
        Colors.Aqua,
        Colors.Coral,
        Colors.Fuchsia,
        Colors.Indigo,
        Colors.Khaki,
        Colors.Lavender,
        Colors.Salmon,
        Colors.MintCream
        };


        private static int currentIndex = -1;

        public static void ResetColorIndex()
        {
            currentIndex = -1;
        }

        public static Color GetNextColor()
        {
            currentIndex = (currentIndex + 1) % colors.Length;
            return colors[currentIndex];
        }
    }
}
