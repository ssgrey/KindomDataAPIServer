using DevExpress.Xpf.Core;
using DevExpress.XtraSpreadsheet.Model;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using KindomDataAPIServer.KindomAPI;
using KindomDataAPIServer.Models;
using KindomDataAPIServer.ViewModels;
using Smt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Web.UI.WebControls;
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
        LogView logView = null;
        public SyncKindomDataViewModel ViewModel { set; get; }
        ApiConfig apiData = null;
        public SyncKindomDataView(string[] args, ApiConfig config)
        {
            InitializeComponent();
            apiData = config;
            Ini(args);
            this.Loaded += SyncKindomDataView_Loaded;
        }

        private void Ini(string[] args)
        {
            logView = new LogView();
            try
            {
                if (args != null && args.Length > 0)
                {
                    string decodedArgs = System.Uri.UnescapeDataString(args[0]);
                    string joinedArgs = string.Join(" ", args);

                    LogManagerService.Instance.LogDebug("Web args:" + decodedArgs);

                    //apiData = Utils.ParseUri(decodedArgs);
                    //var client = ServiceLocator.GetService<IApiClient>();
                    //client.SetHeaders(apiData.token, apiData.tetproj);
                    //if (apiData.port.Contains("30015"))
                    //{
                    //    client.SetBaseUrl($"http://{apiData.ip}:{apiData.port}/tet/");
                    //}
                    //else
                    //{
                    //    client.SetBaseUrl($"https://{apiData.ip}/tet/");
                    //}
                }
                //else
                //{
                //    string decodedArgs = File.ReadAllText("tempArgs.txt");
                //    string joinedArgs = string.Join(" ", args);
                //    apiData = Utils.ParseUri(decodedArgs);
                //    var client = ServiceLocator.GetService<IApiClient>();
                //    client.SetHeaders(apiData.token, apiData.tetproj);
                //    if (apiData.port.Contains("30015"))
                //    {
                //        client.SetBaseUrl($"http://{apiData.ip}:{apiData.port}/tet/");
                //    }
                //    else
                //    {
                //        client.SetBaseUrl($"https://{apiData.ip}:{apiData.port}/tet/");
                //    }
                //}
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log(ex.Message);
            }

        }

        private void SyncKindomDataView_Loaded(object sender, RoutedEventArgs e)
        {
            //Waiter.DeferedVisibility = true;
            ViewModel = new SyncKindomDataViewModel(apiData);
            this.DataContext = ViewModel;
            //Waiter.DeferedVisibility = false;
        }

        private void Open_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (ViewModel.IsInitial)
            {
                DXMessageBox.Show("It's loading unit config from web,please wait...");
                return;
            }
            OpenProjectView openProjectView = new OpenProjectView(ViewModel);
            openProjectView.Owner = this;
            openProjectView.ShowDialog();
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

        private void Exit_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            LogManagerService.Instance.TextBox = null;
            logView.Close();       
            logView = null;
            this.Close();
        }


        private void Log_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if(logView==null)
                logView = new LogView();
           logView.Show();
        }

        LogServerView logServerView = null;
        private void ServerLog_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if(logServerView == null)
             logServerView = new LogServerView();
            logServerView.Show();
        }
    }
}
