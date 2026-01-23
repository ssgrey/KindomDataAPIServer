using DevExpress.Xpf.Core;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using KindomDataAPIServer.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KindomDataAPIServer
{
    static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                ServiceLocator.ConfigureServices();
                // 自定义启动逻辑
                App app = new App();

                SyncKindomDataView syncKindomDataView = new SyncKindomDataView(args, app);
                syncKindomDataView.ShowDialog();
               // app.Run(syncKindomDataView);
            }
            catch (Exception ex)
            {
                string msg = ex.StackTrace + ex.Message;

                if (ex.InnerException != null)
                {
                    msg = msg+ ex.InnerException.Message + ex.InnerException.StackTrace;
                }
                MessageBox.Show(msg);
                LogManagerService.Instance.Log(msg);

            }

        }
    }
}
