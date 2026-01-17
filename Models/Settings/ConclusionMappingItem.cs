using DevExpress.Mvvm;
using DevExpress.Xpf.Editors.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using BindableBase = DevExpress.Mvvm.BindableBase;

namespace KindomDataAPIServer.Models
{

    public class ConclusionMappingItem : BindableBase
    {
        public ConclusionMappingItem()
        {
            GUID = Guid.NewGuid().ToString();
        }

        public bool IsChecked { get; set; } = true;

        public string GUID { get; set; } 
        public string PolygonName { get; set; }

        public System.Windows.Media.Color _Color; 
        public System.Windows.Media.Color Color
        {
            get
            { 
                return _Color;
            }
            set
            {
                SetValue(ref _Color, value, nameof(Color));
            }
        }

        private double _LineWidth = 2;
        public double LineWidth
        {
            get { return _LineWidth; }
            set
            {
                SetValue(ref _LineWidth, value, nameof(LineWidth));

            }
        }
        
        public string SymbolLibraryCode { get; set; }

        private string _SymbolLibraryName;
        public string SymbolLibraryName
        {
            get {  return _SymbolLibraryName; }
            set
            {
                SetValue(ref _SymbolLibraryName, value,nameof(SymbolLibraryName));
            }
        }

        private BitmapImage _Image;
        public BitmapImage Image
        {
            get { return _Image; }
            set
            {
                SetValue(ref _Image, value, nameof(Image));
            }
        }



        private System.Windows.Media.Color? _FillColor;
        public System.Windows.Media.Color FillColor
        {
            get 
            { 
                if (_FillColor == null)
                {
                    _FillColor = Color;
                }
                return _FillColor.Value; 
            }
            set 
            {
                SetValue(ref _FillColor, value, nameof(FillColor));
            }
        }


    }
}
