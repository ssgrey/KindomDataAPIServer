using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;


namespace Tet.GeoSymbol.UI
{
    /// <summary>
    /// Interaction logic for GeologySymbolExplorerWindow.xaml
    /// </summary>
    public partial class GeologySymbolExplorerWindow : ThemedWindow
    {

       
        public GeologySymbolExplorerWindow()
        {
            InitializeComponent();
            this.UpdateOkButtonEnabled();
            this.symbolExplorerView.ViewModel.PropertyChanged += OnSymbolExpolorerChanged;
        }

        private void OnSymbolExpolorerChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(GeologySymbolsExplorerViewModel.IsGeoSymbolSelected))){
                this.UpdateOkButtonEnabled();
            }
        }

        void UpdateOkButtonEnabled()
        {
            btnDialogOk.IsEnabled = this.symbolExplorerView.ViewModel.IsGeoSymbolSelected;
        }


        public GeoSymbolRepository SymbolLib
        {
            get
            {
                return this.symbolExplorerView.ViewModel.SymbolLib;
            }
            set
            {
                this.symbolExplorerView.ViewModel.SymbolLib = value;
            }
        }
        public string[] DisplaySymbolCatalogs
        {
            get
            {
                return this.symbolExplorerView.ViewModel.DisplaySymbolCategoryRanges;
            }
            set
            {
                this.symbolExplorerView.ViewModel.DisplaySymbolCategoryRanges = value;
                this.Dispatcher.InvokeAsync(() => this.symbolExplorerView.SelectDefaultItem());
                
            }
        }

        public GeoSymbolData GetResult()
        {
            
            GeoSymbolData symbolData =  this.symbolExplorerView.ViewModel.SelectedGeoSymbolData;
            GeoSymbolData thumb = new GeoSymbolData();
            thumb.ID = symbolData.ID;
            thumb.Title = symbolData.Title;
            thumb.Image = this.symbolExplorerView.ViewModel.CreateThumbnailImage(thumb.ID);
            thumb.Name = symbolData.Name;
            return thumb;
        }

        public GeoSymbolData GetResult(int size)
        {

            GeoSymbolData symbolData = this.symbolExplorerView.ViewModel.SelectedGeoSymbolData;
            GeoSymbolData thumb = new GeoSymbolData();
            thumb.ID = symbolData.ID;
            thumb.Title = symbolData.Title;
            thumb.Image = this.symbolExplorerView.ViewModel.CreateThumbnailImage(thumb.ID,size);
            thumb.Name = symbolData.Name;
            return thumb;

        }


        private void OnOKClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void OnSymbolViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
            if(this.symbolExplorerView.ViewModel.SelectedGeoSymbolData!= null)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
