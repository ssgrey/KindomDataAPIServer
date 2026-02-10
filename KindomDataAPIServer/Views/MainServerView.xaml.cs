using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
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
using System.Windows.Threading;

namespace KindomDataAPIServer
{
    public partial class MainServerView : Window
    {
        private IDisposable _webApp;
        private const string BaseAddress = "http://localhost:5000/";
        private DispatcherTimer _statusTimer;

        public MainServerView()
        {
            InitializeComponent();
            InitializeStatusTimer();
            Log("应用程序启动完成", "INFO");
        }

        private void InitializeStatusTimer()
        {
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(1);
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            UpdateServiceStatus();
        }

        private void UpdateServiceStatus()
        {
            bool isRunning = _webApp != null;
            txtStatus.Text = isRunning ? "运行中" : "已停止";
            txtStatus.Foreground = isRunning ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            txtServiceUrl.Text = isRunning ? BaseAddress : "未启动";
        }

        private async void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStartServer.IsEnabled = false;
                btnStopServer.IsEnabled = true;

                Log("正在启动WebAPI服务...", "INFO");

                await Task.Run(() =>
                {
                   // _webApp = WebApp.Start<Startup>(BaseAddress);
                });

                Log($"WebAPI服务已成功启动", "SUCCESS");
                Log($"服务地址: {BaseAddress}", "INFO");
                Log($"目录接口: {BaseAddress}api/directory/list", "INFO");
                Log($"驱动器接口: {BaseAddress}api/directory/drives", "INFO");
                Log($"帮助页面: {BaseAddress}help", "INFO");
                Log("--------------------------------------------------", "INFO");
            }
            catch (Exception ex)
            {
                Log($"启动服务失败: {ex.Message}", "ERROR");
                if (ex.InnerException != null)
                {
                    Log($"内部错误: {ex.InnerException.Message}", "ERROR");
                }
                btnStartServer.IsEnabled = true;
                btnStopServer.IsEnabled = false;
            }
        }

        private void btnStopServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_webApp != null)
                {
                    _webApp.Dispose();
                    _webApp = null;
                }

                btnStartServer.IsEnabled = true;
                btnStopServer.IsEnabled = false;

                Log("WebAPI服务已停止", "INFO");
                Log("--------------------------------------------------", "INFO");
            }
            catch (Exception ex)
            {
                Log($"停止服务失败: {ex.Message}", "ERROR");
            }
        }

        private void Log(string message, string level = "INFO")
        {
            Dispatcher.Invoke(() =>
            {
                string levelPrefix = "";
                System.Windows.Media.Brush levelColor = System.Windows.Media.Brushes.Black;

                switch (level)
                {
                    case "INFO":
                        levelPrefix = "[INFO]";
                        levelColor = System.Windows.Media.Brushes.Blue;
                        break;
                    case "SUCCESS":
                        levelPrefix = "[SUCCESS]";
                        levelColor = System.Windows.Media.Brushes.Green;
                        break;
                    case "ERROR":
                        levelPrefix = "[ERROR]";
                        levelColor = System.Windows.Media.Brushes.Red;
                        break;
                    case "WARNING":
                        levelPrefix = "[WARNING]";
                        levelColor = System.Windows.Media.Brushes.Orange;
                        break;
                }

                // 创建带有颜色的文本
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ");

                var levelRun = new System.Windows.Documents.Run(levelPrefix)
                {
                    Foreground = levelColor
                };
                txtLog.AppendText(levelPrefix);

                txtLog.AppendText($" {message}{Environment.NewLine}");
                txtLog.ScrollToEnd();
            });
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
            Log("日志已清空", "INFO");
        }

        private void btnCopyLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(txtLog.Text);
                Log("日志已复制到剪贴板", "INFO");
            }
            catch (Exception ex)
            {
                Log($"复制失败: {ex.Message}", "ERROR");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_webApp != null)
            {
                var result = MessageBox.Show("服务正在运行，确定要退出吗？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _webApp.Dispose();
                }
                else
                {
                    e.Cancel = true;
                }
            }

            _statusTimer?.Stop();
        }
    }
}
