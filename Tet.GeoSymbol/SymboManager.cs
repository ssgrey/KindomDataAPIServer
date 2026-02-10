using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tet.GeoSymbol
{
    public class SymboManager
    {
        private static GeoSymbolRepository _symbolLib;
        public static GeoSymbolRepository GeoSymbolLib
        {
            get
            {
                if (_symbolLib == null)
                {
                    String pathToFileName = System.AppDomain.CurrentDomain.BaseDirectory + "geosymbol\\GeoSymbol_en-US.lib";
                    if (!File.Exists(pathToFileName))
                    {
                        throw new FileNotFoundException(String.Format("Symbol Library File Not Exist:\"{0}\"", pathToFileName));
                    }
                    GeoSymbolRepositoryLoader loader = new GeoSymbolRepositoryLoader();
                    _symbolLib = loader.LoadFromFile(pathToFileName, Encoding.GetEncoding("gb2312"));

                }
                return _symbolLib;
            }
        }
    }
}
