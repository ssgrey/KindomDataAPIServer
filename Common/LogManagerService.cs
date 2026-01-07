using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Common
{
    public class LogManagerService
    {
        private static readonly Lazy<LogManagerService> _instance = new Lazy<LogManagerService>(() => new LogManagerService());
        private readonly string _logFilePath;

        private LogManagerService()
        {
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            EnsureLogFileIsToday();
        }

        public static LogManagerService Instance => _instance.Value;

        /// <summary>
        /// 调试模式
        /// </summary>
        public bool IsDebugMode { get; set; } = true;
        public System.Windows.Controls.RichTextBox TextBox { get; set; }
        public void Log(string message)
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}";
            try
            {
                EnsureLogFileIsToday();
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
                if(TextBox != null)
                {
                    TextBox.Dispatcher.Invoke(() =>
                    {
                        TextBox.AppendText(logEntry);
                        TextBox.AppendText(Environment.NewLine);
                        TextBox.ScrollToEnd();
                    });
                }
            }
            catch
            {
                // 可根据需要处理异常
            }
        }


        public void LogDebug(string message)
        {
            if(IsDebugMode)
                 Log(message);
        }

        /// <summary>
        /// 检查日志文件的最后修改日期，如果不是今天则删除并重新创建
        /// </summary>
        private void EnsureLogFileIsToday()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var lastWrite = File.GetLastWriteTime(_logFilePath);
                    if (lastWrite.Date != DateTime.Today)
                    {
                        File.Delete(_logFilePath);
                        // 创建新文件
                        File.WriteAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd}] 日志文件已重置{Environment.NewLine}", Encoding.UTF8);
                    }
                }
                else
                {
                    // 创建新文件
                    File.WriteAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd}] 日志文件已创建{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch
            {
                // 可根据需要处理异常
            }
        }
    }
}
