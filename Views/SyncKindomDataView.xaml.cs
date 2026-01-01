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
    public partial class SyncKindomDataView : ThemedWindow
    {
        SyncKindomDataViewModel ViewModel = null;
        public SyncKindomDataView(string[] args)
        {
            InitializeComponent();
            Ini(args);
        }

        private void Ini(string[] args)
        {
            ViewModel = new SyncKindomDataViewModel();
            this.DataContext = ViewModel;
            LogManagerService.Instance.TextBox = logger;

            try
            {
                if (args != null && args.Length > 0)
                {
                    string decodedArgs = System.Uri.UnescapeDataString(args[0]);
                    string joinedArgs = string.Join(" ", args);

                    //LogManagerService.Instance.Log("Web args:" +  decodedArgs);

                    var apiData = Utils.ParseUri(decodedArgs);
                    var client = ServiceLocator.GetService<IApiClient>();
                    client.SetHeaders(apiData.Token, apiData.Tetproj);
                    client.SetBaseUrl($"http://{apiData.Ip}:{apiData.Port}/tet/");
                }
                else
                {
                    string decodedArgs = File.ReadAllText("tempArgs.txt");
                    string joinedArgs = string.Join(" ", args);
                    var apiData = Utils.ParseUri(decodedArgs);
                    var client = ServiceLocator.GetService<IApiClient>();
                    client.SetHeaders(apiData.Token, apiData.Tetproj);
                    client.SetBaseUrl($"http://{apiData.Ip}:{apiData.Port}/tet/");
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log(ex.Message);
            }
        }

        private void Logger_Clean(object sender, RoutedEventArgs e)
        {
            logger.Document.Blocks.Clear();
        }

        private void OnDragDeta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (sender is GridSplitter sp)
            {
                this.logger.Height = double.NaN;
            }
        }

        private void root_Closed(object sender, EventArgs e)
        {
            KingdomAPI.Instance.Close();
        }
    }
}
