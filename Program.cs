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
using System.Windows;

namespace KindomDataAPIServer
{
    static class Program
    {
        private const string AppMutexName = "YourAppName_UniqueMutex_12345";
        private const string PipeName = "YourAppName_Pipe_12345";
        private static Mutex _appMutex;



        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                ServiceLocator.ConfigureServices();
                App app = new App();
                SyncKindomDataView syncKindomDataView = new SyncKindomDataView(args);
                // 设置主窗口
                Application.Current.MainWindow = syncKindomDataView;

                // 设置关闭模式为当主窗口关闭时退出
                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.Run(syncKindomDataView);

            }
            catch (Exception ex)
            {
                string msg = ex.StackTrace + ex.Message;

                if (ex.InnerException != null)
                {
                    msg = msg + ex.InnerException.Message + ex.InnerException.StackTrace;
                }
                MessageBox.Show(msg);
                LogManagerService.Instance.Log(msg);

            }
        }
        public static void Main234(string[] args)
        {
            try
            {
                  ServiceLocator.ConfigureServices();

                SyncKindomDataView syncKindomDataView = new SyncKindomDataView(args);
                syncKindomDataView.ShowDialog();

                //// 1. 尝试创建并获取互斥锁所有权
                //bool isFirstInstance;
                //_appMutex = new Mutex(true, AppMutexName, out isFirstInstance);

                //if (isFirstInstance)
                //{
                //    // 第一个实例：初始化服务并启动管道服务器
                //    ServiceLocator.ConfigureServices();

                //    // 启动管道服务器（异步，不阻塞）
                //    Task.Run(() => StartNamedPipeServer());

                //    // 正常启动主窗口，并传入当前参数
                //    SyncKindomDataView syncKindomDataView = new SyncKindomDataView(args);
                //     // Application.Run(syncKindomDataView); // 使用 Application.Run 启动消息循环
                //    syncKindomDataView.ShowDialog();
                //    // 程序退出时释放Mutex
                //    _appMutex.ReleaseMutex();
                //    _appMutex.Dispose();
                //}
                //else
                //{
                //    DXMessageBox.Show("Another instance is already running. Sending arguments to the first instance.");
                //    // 后续实例：通过管道将参数发送给第一个实例
                //    SendArgumentsToFirstInstance(args);
                //    // 当前进程直接退出，不启动任何窗口
                //}
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

        // 2. 命名管道服务器（主实例监听）
        private static void StartNamedPipeServer()
        {
           //while (true) // 持续监听
            {
                using (var server = new NamedPipeServerStream(
                    PipeName,
                                  PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, // 允许多个连接排队
                PipeTransmissionMode.Message, // 使用消息模式
                PipeOptions.Asynchronous | PipeOptions.WriteThrough))
                {
                    // // 等待客户端连接
                     server.WaitForConnection();
                    DXMessageBox.Show($"客户端连接");

                    // 创建读取器
                    using (StreamReader reader = new StreamReader(server, Encoding.UTF8))
                    {

                        while (server.IsConnected)
                        {
                            // 读取客户端消息
                            string message = reader.ReadLine();
                            if (!string.IsNullOrEmpty(message))
                            {
                                DXMessageBox.Show($"收到客户端消息: {message}");

                                // 如果是退出命令，则结束
                                if (message.ToLower() == "exit")
                                {
                                    break;
                                }
                            }
                            else
                            {
                                Thread.Sleep(100);
                            }
                        }
                    }

                    // DXMessageBox.Show("Client connected to pipe server.");
                    // // 读取客户端发送的数据
                    // byte[] buffer = new byte[4096];
                    // int bytesRead = server.Read(buffer, 0, buffer.Length);
                    // string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    //DXMessageBox.Show("Received data from another instance: " + receivedData);
                    //// 在主线程上处理接收到的参数（因为可能涉及UI更新）
                    //if (Application.OpenForms.Count > 0)
                    //{
                    //    var mainForm = Application.OpenForms[0] as SyncKindomDataView;
                    //    mainForm?.Invoke(new Action(() =>
                    //    {
                    //        // 调用主窗口的方法来处理新参数
                    //        mainForm.ProcessNewArguments(receivedData.Split('|'));
                    //    }));
                    //}

                    server.Disconnect();
                }
            }
        }

        // 3. 客户端发送参数到主实例
        private static void SendArgumentsToFirstInstance(string[] args)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    // 设置3秒连接超时
                    client.Connect(3000);

                    if (client.IsConnected)
                    {
                        DXMessageBox.Show("Connected to pipe server.");
                        // 将参数数组序列化为字符串（用|分隔）
                        string message = args.Length > 0 ? string.Join("|", args) : "PING1231231242141242141242142142141";
                        byte[] buffer = Encoding.UTF8.GetBytes(message);
                        DXMessageBox.Show("Sending data to pipe server: " + message);
                        client.Write(buffer, 0, buffer.Length);
                        client.Flush();
                        DXMessageBox.Show("Arguments sent to the first instance successfully.");
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

    static class Program2
    {

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                ServiceLocator.ConfigureServices();

                SyncKindomDataView syncKindomDataView = new SyncKindomDataView(args);
                syncKindomDataView.ShowDialog();

            }
            catch (Exception ex)
            {
                string msg = ex.StackTrace + ex.Message;

                if (ex.InnerException != null)
                {
                    msg = msg + ex.InnerException.Message + ex.InnerException.StackTrace;
                }
                MessageBox.Show(msg);
                LogManagerService.Instance.Log(msg);

            }

        }

        public static bool IniConfig(string[] args)
        {
            try
            {
                if (args != null && args.Length > 0)
                {
                    string decodedArgs = System.Uri.UnescapeDataString(args[0]);
                    string joinedArgs = string.Join(" ", args);

                    LogManagerService.Instance.LogDebug("Web args:" + decodedArgs);

                    ApiConfig apiData = Utils.ParseUri(decodedArgs);
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
                    ApiConfig apiData = Utils.ParseUri(decodedArgs);
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
                DXMessageBox.Show(ex.Message);
            }
            return true;
        }
    }
}
