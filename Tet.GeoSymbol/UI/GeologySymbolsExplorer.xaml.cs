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

namespace Tet.GeoSymbol.UI
{
    /// <summary>
    /// Interaction logic for GeologySymbolsExplorer.xaml
    /// </summary>
    public partial class GeologySymbolsExplorer : UserControl
    {
        public GeologySymbolsExplorer()
        {
            InitializeComponent();
        }

        public GeologySymbolsExplorerViewModel  ViewModel
        {
            get
            {
               return  this.DataContext as GeologySymbolsExplorerViewModel;
            }
        }

        public void SelectDefaultItem()
        {

            object selectedItem = this.catalogTreeView.SelectedItem;
            if (selectedItem == null)
            {
                if (this.catalogTreeView.Items.Count > 0)
                {
                    object first = this.catalogTreeView.Items[0];
                    //this.ViewModel.SelectedSymbolCatalog = first as SymbolCatalog;
                    var firstObj = this.catalogTreeView.ItemContainerGenerator.ContainerFromItem(first);
                    if (firstObj != null)
                    {
                        TreeViewItem tvi = firstObj as TreeViewItem;
                        tvi.IsSelected = true;
                    }
                }
            }

        }

        private void OnSelectedSymbolCatalogChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
             SymbolCatalog symbolCatalog =  e.NewValue as SymbolCatalog;
             this.ViewModel.SelectedSymbolCatalog = symbolCatalog;  
        }

        
    }
}
