using DevExpress.Xpf.Core;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using KindomDataAPIServer.Models;
using KindomDataAPIServer.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;

namespace KindomDataAPIServer
{
    static class Program
    {
        private const string AppMutexName = "YourAppName_UniqueMutex_12345";
        private const string PipeName = "YourAppName_Pipe_12345";
        private static Mutex _appMutex;

        private static ApiConfig config = null;

        private static SyncKindomDataView MainWindow = null;

       [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // 1. 尝试创建并获取互斥锁所有权
                bool isFirstInstance = true;
                //_appMutex = new Mutex(true, AppMutexName, out isFirstInstance);
                if (isFirstInstance)
                {
                    // 第一个实例：初始化服务并启动管道服务器
                    ServiceLocator.ConfigureServices();

                    config = IniConfig(args);

                    // 启动管道服务器（异步，不阻塞）
                    StartNamedPipeServer();

                    App app = new App();
                    SyncKindomDataView MainWindow = new SyncKindomDataView(args, config);
                    // 设置主窗口
                    Application.Current.MainWindow = MainWindow;
                    // 设置关闭模式为当主窗口关闭时退出
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    app.Run(MainWindow);

                    // 程序退出时释放Mutex
                    //_appMutex.ReleaseMutex();
                    //_appMutex.Dispose();
                }
                else
                {
                    //DXMessageBox.Show("Another instance is already running. Sending arguments to the first instance.");
                    // 后续实例：通过管道将参数发送给第一个实例
                    if(args != null && args.Length > 0)
                    {
                        SendArgumentsToFirstInstance(args[0]);
                    }

                    // 当前进程直接退出，不启动任何窗口
                }
            }
            catch (Exception ex)
            {
                string msg = ex.StackTrace + ex.Message;

                if (ex.InnerException != null)
                {
                    msg = msg + ex.InnerException.Message + ex.InnerException.StackTrace;
                }
                MessageBox.Show(msg);
            }
        }

        private static ApiConfig IniConfig(string[] args)
        {
            ApiConfig apiData = null;
            try
            {
                if (args != null && args.Length > 0)
                {
                    string decodedArgs = System.Uri.UnescapeDataString(args[0]);
                    string joinedArgs = string.Join(" ", args);
                    apiData = Utils.ParseUri(decodedArgs);
                    var client = ServiceLocator.GetService<IApiClient>();
                    client.SetHeaders(apiData.token, apiData.tetproj);
                    if (apiData.port.Contains("30015"))
                    {
                        client.SetBaseUrl($"http://{apiData.ip}:{apiData.port}/tet/");
                    }
                    else
                    {
                        client.SetBaseUrl($"https://{apiData.ip}/tet/");
                    }
                }
                else
                {
                    string decodedArgs = File.ReadAllText("tempArgs.txt");
                    string joinedArgs = string.Join(" ", args);
                    apiData = Utils.ParseUri(decodedArgs);
                    var client = ServiceLocator.GetService<IApiClient>();
                    client.SetHeaders(apiData.token, apiData.tetproj);
                    if (apiData.port.Contains("30015"))
                    {
                        client.SetBaseUrl($"http://{apiData.ip}:{apiData.port}/tet/");
                    }
                    else
                    {
                        client.SetBaseUrl($"https://{apiData.ip}:{apiData.port}/tet/");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log(ex.Message);
            }
            return apiData;
        }


        // 2. 命名管道服务器（主实例监听）
        private static void StartNamedPipeServer()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        using (var server = new NamedPipeServerStream(
                            PipeName,
                            PipeDirection.In,
                            1,
                            PipeTransmissionMode.Message,
                            PipeOptions.None))
                        {
                            server.WaitForConnection();

                            using (var reader = new StreamReader(server))
                            {
                                string msg = reader.ReadToEnd();

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                     DXMessageBox.Show("Received data from another instance: " + msg);
                                });
                            }
                        }
                    }
                    catch
                    {
                        // 可加日志，避免管道异常导致线程退出
                    }
                }
            });
        }

        // 3. 客户端发送参数到主实例
        private static void SendArgumentsToFirstInstance(string args)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(1000); // 1 秒超时
                  
                    using (var writer = new StreamWriter(client))
                    {
                        writer.Write(args);
                        writer.Flush();
                    }
                }
            }
            catch (TimeoutException ex)
            {
                DXMessageBox.Show("Failed to connect to the first instance: " + ex.Message);
                // 连接超时，可能主实例还没准备好，可以选择忽略或提示
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("Error sending arguments to the first instance: " + ex.Message+ ex.StackTrace);
                // 其他异常，主实例可能已异常退出
            }
        }
    }
}
