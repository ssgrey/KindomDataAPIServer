using DevExpress.Xpf.Core;
using DevExpress.XtraSpreadsheet.Model;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using KindomDataAPIServer.KindomAPI;
using KindomDataAPIServer.ViewModels;
using Smt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace KindomDataAPIServer.Views
{
    /// <summary>
    /// Interaction logic for SyncKindomDataView.xaml
    /// </summary>
    public partial class OpenProjectView : ThemedWindow
    {
        SyncKindomDataViewModel ViewModel = null;
        public OpenProjectView(SyncKindomDataViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            ViewModel.LoginGridVisiable = Visibility.Visible;
            this.DataContext = ViewModel;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
