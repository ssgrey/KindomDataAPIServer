using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Tet.GeoSymbol.UI
{
    public class SymbolCatalog
    {
        public SymbolCatalog()
        {
            this.Children = new List<SymbolCatalog>();
        }
        public List<SymbolCatalog> Children { get; set; }

        public string ID { get; set; }
        public string Name { get; set; }


        public const string ID_USGS = "0";
        public const string ID_GEOGRAPHY = "1";
        public const string ID_ROCK = "2";
        public const string ID_STRUCTURE = "3";
        public const string ID_OIL_GAS = "4";
        public const string ID_WELL_CATEGORY_STATUS = "5";
        public const string ID_SEDIMENTARY = "6";
        public const string ID_SIDEWALL_CORES = "7";
        public const string ID_MISC_SYMBOLE = "9";

        public static string[] ALL_CATEGORIES = { ID_USGS,ID_GEOGRAPHY, ID_ROCK, ID_STRUCTURE, ID_OIL_GAS, ID_WELL_CATEGORY_STATUS, ID_SEDIMENTARY, ID_SIDEWALL_CORES, ID_MISC_SYMBOLE };
        public static string[] IMPORT_LITHOLOGY = { ID_ROCK, ID_OIL_GAS, ID_SEDIMENTARY };
        public static string[] IMPORT_PAYZONE = { ID_OIL_GAS };
        public static string[] IMPORT_SEDIMENTARY_FACES = { ID_SEDIMENTARY };
    }


    public class GeoSymbolData
    {
        public BitmapImage Image { get; set; }
        public string Title { get; set; }

        public string Name { get; set; }
        public string ID { get; set; }

    }
}
