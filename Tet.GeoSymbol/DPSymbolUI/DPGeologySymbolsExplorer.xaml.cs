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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tet.GeoSymbol.DPSymbolUI;

namespace Tet.GeoSymbol.UI
{
    /// <summary>
    /// Interaction logic for GeologySymbolsExplorer.xaml
    /// </summary>
    public partial class DPGeologySymbolsExplorer : Window
    {
        SymbolType _symbolType = SymbolType.Payzon;
        public List<SymbolItem> SymbolItems { get; set; }

        public DPGeologySymbolsExplorer(SymbolType symbolType)
        {
            InitializeComponent();
            _symbolType = symbolType;
            Ini();
        }


        public void Ini()
        {
            try
            {
                if (_symbolType == SymbolType.Payzon)
                {
                    SymbolItems = SymbolDataManager.SymbolData.PayzoneItems;
                    this.Title = "Payzon";
                }
                else if (_symbolType == SymbolType.Lithology)
                {
                    SymbolItems = SymbolDataManager.SymbolData.LithologyItems;
                    this.Title = "Lithology";
                }
                else if (_symbolType == SymbolType.Facies)
                {
                    SymbolItems = SymbolDataManager.SymbolData.FaciesItems;
                    this.Title = "Facies";
                }
                this.listView.ItemsSource = SymbolItems;
                listView.SelectedItem = SymbolItems.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }

        }

        public SymbolItem GetSelectItem()
        {
            object selectedItem = this.listView.SelectedItem;
            if(selectedItem is SymbolItem symbol)
            {
                return symbol;
            }
            else
            {
                return SymbolItems.FirstOrDefault();
            }
        }
        public void SelectDefaultItem()
        {
            object selectedItem = this.listView.SelectedItem;
            if (selectedItem == null)
            {
                if (this.listView.Items.Count > 0)
                {
                    object first = this.listView.Items[0];
                    //this.ViewModel.SelectedSymbolCatalog = first as SymbolCatalog;
                    var firstObj = this.listView.ItemContainerGenerator.ContainerFromItem(first);
                    if (firstObj != null)
                    {
                        TreeViewItem tvi = firstObj as TreeViewItem;
                        tvi.IsSelected = true;
                    }
                }
            }
        }

        private void OnOKClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
