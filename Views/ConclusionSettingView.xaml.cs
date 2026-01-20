using DevExpress.Charts.Native;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.Office.Utils;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using KindomDataAPIServer.KindomAPI;
using KindomDataAPIServer.Models;
using KindomDataAPIServer.ViewModels;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Tet.GeoSymbol;
using Tet.GeoSymbol.UI;

namespace KindomDataAPIServer.Views
{
    /// <summary>
    /// Interaction logic for DVChartPointSettings.xaml
    /// </summary>
    public partial class ConclusionSettingView : UserControl
    {
        public SyncKindomDataViewModel ViewModel { get; private set; }

        public ConclusionSettingView()
        {
            InitializeComponent();
            GridControl.AllowInfiniteGridSize = true;
        }

        private void Reload_Clicked(object sender, RoutedEventArgs e)
        {
            //ViewModel.ConclusionSettingVM.ConclusionMappingItems = new System.Collections.ObjectModel.ObservableCollection<ConclusionMappingItem>();
            //List<string> ConclusionNames = KingdomAPI.Instance.GetConclusionNames(ViewModel.KindomData);
            //foreach (var item in ConclusionNames)
            //{
            //    ConclusionMappingItem conclusionMappingItem = new ConclusionMappingItem()
            //    {
            //        Color = Colors.Red,
            //        PolygonName = item,
            //    };
            //    ViewModel.ConclusionSettingVM.ConclusionMappingItems.Add(conclusionMappingItem);
            //}
        }


        private void Apply_Clicked(object sender, RoutedEventArgs e)
        {

        }

        string[] _displayCatalogs = new string[0];
        public string[] DisplayCatalogs
        {
            get { return _displayCatalogs; }
            set { _displayCatalogs = value; }
        }

        private void conclusionTableView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            IList<GridCell> gridCells = this.conclusionTableView.GetSelectedCells();
            if (gridCells.Count > 0)
            {
                GridCell gridCell = gridCells[0];
                var conclusionMapping = gridCell.Row as ConclusionMappingItem;
                if (gridCell.Column.FieldName == "SymbolLibraryName")
                {
                    //ViewModel
                    GeoSymbolRepository geoSymbolLib = SymboManager.GeoSymbolLib;

                    var symbolNodes = geoSymbolLib.SymbolNodes;
                    if (this.DisplayCatalogs == null || this.DisplayCatalogs.Length == 0)
                    {
                        this.DisplayCatalogs = symbolNodes.Select(node => node.m_sCode).ToArray();
                    }

                    GeologySymbolExplorerWindow explorer = new GeologySymbolExplorerWindow
                    {
                        Title = "Symbol Explorer",
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        SymbolLib = geoSymbolLib,
                        DisplaySymbolCatalogs = this.DisplayCatalogs
                    };

                    bool? result = explorer.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        GeoSymbolData symbolData = explorer.GetResult();
                        if (symbolData != null)
                        {
                            conclusionMapping.SymbolLibraryName = symbolData.Title;
                            //conclusionMapping.Image = CreateConclusionImage(symbolData.ID, Colors.Transparent);
                            conclusionMapping.SymbolLibraryCode = symbolData.ID;
                        }
                    }
                    else
                    {
                        Console.WriteLine("No symbol was selected.");
                    }


                }
            }
        }



        //private BitmapImage CreateConclusionImage(string symboId, Color? backcolor)
        //{
        //    if (string.IsNullOrEmpty(symboId))
        //        return null;

        //    System.Drawing.Color? destColor = null;
        //    if (backcolor != null)
        //    {
        //        destColor = System.Drawing.Color.FromArgb(backcolor.Value.A, backcolor.Value.R, backcolor.Value.G, backcolor.Value.B);
        //    }
        //    var image = Project.Current.GeoSymbolLib.CreateSymbolImage(symboId, 64, false, destColor);
        //    BitmapImage result = ImageConverter.Convert(image);
        //    image.Dispose();
        //    return result;
        //}

    }
}
