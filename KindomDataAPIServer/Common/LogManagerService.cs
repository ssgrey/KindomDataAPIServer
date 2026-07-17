using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace KindomDataAPIServer.Common
{
    public class LogManagerService
    {
        private const int RetainedLogDays = 7;
        private const string LogFilePrefix = "app-";
        private const string LogFileExtension = ".log";

        private static readonly Lazy<LogManagerService> _instance = new Lazy<LogManagerService>(() => new LogManagerService());
        private readonly object _syncRoot = new object();
        private readonly string _logDirectory;
        private DateTime _lastCleanupDate = DateTime.MinValue;

        private LogManagerService()
        {
            _logDirectory = AppDomain.CurrentDomain.BaseDirectory;
            EnsureLogFile(DateTime.Today);
            CleanupExpiredLogFiles(DateTime.Today);
        }

        public static LogManagerService Instance => _instance.Value;

        /// <summary>
        /// 调试模式
        /// </summary>
        public bool IsDebugMode { get; set; } = true;
        public System.Windows.Controls.RichTextBox TextBox { get; set; }
        public void Log(string message)
        {
            var now = DateTime.Now;
            var logEntry = $"{now:yyyy-MM-dd HH:mm:ss.fff} | {message}";
            try
            {
                if(TextBox != null)
                {
                    TextBox.Dispatcher.Invoke(() =>
                    {
                        TextBox.AppendText(logEntry);
                        TextBox.AppendText(Environment.NewLine);
                        TextBox.ScrollToEnd();
                    });
                }

                lock (_syncRoot)
                {
                    EnsureLogFile(now.Date);
                    CleanupExpiredLogFiles(now.Date);
                    File.AppendAllText(GetLogFilePath(now.Date), logEntry + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch(Exception ex)
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
        /// 确保当天日志文件存在
        /// </summary>
        private void EnsureLogFile(DateTime logDate)
        {
            try
            {
                var logFilePath = GetLogFilePath(logDate);
                if (!File.Exists(logFilePath))
                {
                    File.WriteAllText(logFilePath, $"[{logDate:yyyy-MM-dd}] 日志文件已创建{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch(Exception ex)
            {
                //DXMessageBox.Show("日志文件初始化失败: " + ex.Message + ex.StackTrace);
                // 可根据需要处理异常
            }
        }

        /// <summary>
        /// 清理超过最近一周的日志文件
        /// </summary>
        private void CleanupExpiredLogFiles(DateTime today)
        {
            if (_lastCleanupDate == today)
            {
                return;
            }

            try
            {
                var earliestRetainedDate = today.AddDays(1 - RetainedLogDays);
                foreach (var logFilePath in Directory.EnumerateFiles(_logDirectory, LogFilePrefix + "*" + LogFileExtension))
                {
                    var fileName = Path.GetFileNameWithoutExtension(logFilePath);
                    var datePart = fileName.Substring(LogFilePrefix.Length);
                    DateTime logDate;
                    if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out logDate)
                        && logDate.Date < earliestRetainedDate)
                    {
                        File.Delete(logFilePath);
                    }
                }

                _lastCleanupDate = today;
            }
            catch(Exception ex)
            {
                //DXMessageBox.Show("日志文件清理失败: " + ex.Message + ex.StackTrace);
                // 可根据需要处理异常
            }
        }

        private string GetLogFilePath(DateTime logDate)
        {
            return Path.Combine(_logDirectory, $"{LogFilePrefix}{logDate:yyyy-MM-dd}{LogFileExtension}");
        }
    }
}
