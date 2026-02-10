using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using DevExpress.Mvvm;

namespace Tet.GeoSymbol.UI
{
    public class GeologySymbolsExplorerViewModel : ViewModelBase
    {

        private List<SymbolCatalog> _symbolCatalogs = new List<SymbolCatalog>();
        private GeoSymbolRepository _symbolLib = null;
        private SymbolCatalog       _selectedSymbolCatalog = null;
        //显示的范围
        private string[] _dispSymbolRanges = { };
        private List<GeoSymbolData> _currentGeoSymbols = new List<GeoSymbolData>();
        private GeoSymbolData _geoSymbolData = null;
        private bool _isGeoSymboDataSelected = false;
        private int _itemColumns = 2;
        private int _itemRows = 10;
        
        


        public GeoSymbolRepository SymbolLib
        {
            get
            {
                return _symbolLib;
            }
            set
            {
                this.SetProperty<GeoSymbolRepository>(ref _symbolLib, value, nameof(SymbolLib));
            }
        }

        public List<SymbolCatalog> SymbolCatalogs
        {
            get
            {
                return _symbolCatalogs;
            }
            set
            {
                bool changed =this.SetProperty<List<SymbolCatalog>>(ref _symbolCatalogs, value, nameof(SymbolCatalogs));
                if (changed)
                {
                    this.SelectedGeoSymbolData = null;
                }
            }
        }

        public SymbolCatalog SelectedSymbolCatalog
        {
            get
            {
                return _selectedSymbolCatalog;
            }
            set
            {
                this.SetProperty<SymbolCatalog>(ref _selectedSymbolCatalog, value, nameof(SelectedSymbolCatalog));

                List<GeoSymbolData> symbolDataList = this.CreateSymbolCatalogSymbolDataList(value);
                int columns = 2;
                int rows = symbolDataList.Count / columns;
                int remainder = symbolDataList.Count % columns;
                if (remainder > 0)
                    rows = rows + 1;
                if (rows < 10)
                    rows = 10;


                this.ItemColumns = columns;
                this.ItemRows = rows;
                this.CurrentCatalogSymbolDataList = symbolDataList;
            }
        }

        public List<GeoSymbolData> CurrentCatalogSymbolDataList
        {
            get
            {
                return _currentGeoSymbols;
            }
            set
            {
                this.SetProperty<List<GeoSymbolData>>(ref _currentGeoSymbols, value, nameof(CurrentCatalogSymbolDataList));
            }
        }

        public int ItemRows
        {
            get
            {
                return _itemRows;
            }
            set
            {
                this.SetProperty<int>(ref _itemRows, value, nameof(ItemRows));
            }
        }

        public int ItemColumns
        {
            get
            {
                return _itemColumns;
            }
            set
            {
                this.SetProperty<int>(ref _itemColumns, value, nameof(ItemColumns));
            }
        }


        public GeoSymbolData SelectedGeoSymbolData
        {
            get
            {
                return _geoSymbolData;
            }
            set
            {
                bool changed = this.SetProperty<GeoSymbolData>(ref _geoSymbolData, value, nameof(SelectedGeoSymbolData));
                if (changed)
                {
                    if(value != null)
                    {
                        
                        this.IsGeoSymbolSelected = true;
                    }
                    else
                    {
                        this.IsGeoSymbolSelected = false;
                    }
                }
            }
        }


        public bool IsGeoSymbolSelected
        {
            get
            {
                return _isGeoSymboDataSelected;
            }
            set
            {
                this.SetProperty<bool>(ref _isGeoSymboDataSelected, value, nameof(IsGeoSymbolSelected));
            }
        }





        public string[] DisplaySymbolCategoryRanges
        {
            get
            {
                return _dispSymbolRanges;
            }
            set
            {
                this.SetProperty<string[]>(ref _dispSymbolRanges, value, nameof(DisplaySymbolCategoryRanges));
                List<SymbolCatalog> symbolCatalogs = this.CreateSymbolCatalog();
                this.SymbolCatalogs = symbolCatalogs;
            }
        }

        protected bool ThumbnailImage()
        {
            return false;
        }

        protected List<GeoSymbolData> CreateSymbolCatalogSymbolDataList(SymbolCatalog symbolCatalog)
        {
            List<GeoSymbolData> symbolResults = new List<GeoSymbolData>();
            if(symbolCatalog == null||_symbolLib == null)
            {
                return symbolResults;
            }

            List<string> symCodes = _symbolLib.SearchSymbolCode(symbolCatalog.ID);
            for(int i=0; i<symCodes.Count; i++)
            {
                string code = symCodes[i];
                SymData? symbolInfo = _symbolLib.FindSymbol(symCodes[i]);
                GeoSymbolData symbolData = new GeoSymbolData();
                symbolData.Title = symbolInfo.Value.NameCN;
                symbolData.Name = symbolInfo.Value.Name;
                symbolData.ID = code;
                int width = 64;
                Image image = _symbolLib.CreateSymbolImage(code,width,true);
                BitmapImage destImage = ImageHelper.GetThumbnailImage(image, width);
                image.Dispose();
                image = null;
                BitmapImage bitMapImage = destImage;
                symbolData.Image = bitMapImage;
                symbolResults.Add(symbolData);
            }
            return symbolResults;
        }
       

        protected List<SymbolCatalog> CreateSymbolCatalog()
        {
            List<SymbolCatalog> result = new List<SymbolCatalog>();
            List<SymbolNode>    symbolNodes = _symbolLib.SymbolNodes;
            for(int i=0; i<symbolNodes.Count; i++)
            {
                SymbolNode symNode = symbolNodes[i];
                string code = symNode.m_sCode;
                if (_dispSymbolRanges.Contains<string>(code))
                {
                    SymbolCatalog symbolCatalog = CreateSymbolCatalogRecursively(symNode);
                    result.Add(symbolCatalog);
                }
            }
            return result;
        }

        public  BitmapImage CreateThumbnailImage(string symcode)
        {
            Image image = _symbolLib.CreateSymbolImage(symcode, 32,true);
            BitmapImage result = ImageConverter.Convert(image);
            image.Dispose();
            return result;
        }

        public BitmapImage CreateThumbnailImage(string symcode, int size)
        {
            Image image = _symbolLib.CreateSymbolImage(symcode, size,true);
            BitmapImage result = ImageConverter.Convert(image);
            image.Dispose();
            return result;
        }

        private SymbolCatalog CreateSymbolCatalogRecursively(SymbolNode node)
        {
            SymbolCatalog symbolCatalog = new SymbolCatalog();
            symbolCatalog.ID = node.m_sCode;
            symbolCatalog.Name = node.m_sNameCN;
            if (node.m_children.Count > 0)
            {
                for (int i = 0; i < node.m_children.Count; i++)
                {
                    SymbolNode childNode = node.m_children[i];
                    if (childNode.m_children!= null && childNode.m_children.Count > 0)
                    {
                        SymbolCatalog childCatalog = CreateSymbolCatalogRecursively(childNode);
                        symbolCatalog.Children.Add(childCatalog);
                    }
                }
            }
            return symbolCatalog;
        }



    }
}