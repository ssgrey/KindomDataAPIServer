using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace KindomDataAPIServer.Common
{
    public class LogManagerService
    {
        private const int RetainedLogDays = 7;
        private const int MaxPendingUiLogLines = 20000;
        private const string LogDirectoryName = "Logs";
        private const string AppDataDirectoryName = "KindomDataAPIServer";
        private const string LogFilePrefix = "app-";
        private const string LogFileExtension = ".log";

        private static readonly Lazy<LogManagerService> _instance = new Lazy<LogManagerService>(() => new LogManagerService());
        private readonly object _fileSyncRoot = new object();
        private readonly object _uiSyncRoot = new object();
        private readonly Queue<string> _pendingUiLogs = new Queue<string>();
        private readonly string _logDirectory;
        private readonly bool _isFileLoggingEnabled;
        private DateTime _lastCleanupDate = DateTime.MinValue;

        private LogManagerService()
        {
            _logDirectory = ResolveLogDirectory();
            _isFileLoggingEnabled = !string.IsNullOrWhiteSpace(_logDirectory);
            if (_isFileLoggingEnabled)
            {
                EnsureLogFile(DateTime.Now);
                CleanupExpiredLogFiles(DateTime.Now);
            }
        }

        public static LogManagerService Instance => _instance.Value;

        /// <summary>
        /// 调试模式
        /// </summary>
        public bool IsDebugMode { get; set; } = true;

        public void Log(string message)
        {
            var now = DateTime.Now;
            var logEntry = $"{now:yyyy-MM-dd HH:mm:ss.fff} | {message}";
            try
            {
                EnqueueUiLog(logEntry);

                if (_isFileLoggingEnabled)
                {
                    lock (_fileSyncRoot)
                    {
                        EnsureLogFile(now);
                        CleanupExpiredLogFiles(now);
                        File.AppendAllText(GetLogFilePath(now), logEntry + Environment.NewLine, Encoding.UTF8);
                    }
                }
            }
            catch(Exception)
            {
                // 可根据需要处理异常
            }
        }

        public List<string> DequeueUiLogs()
        {
            lock (_uiSyncRoot)
            {
                var logs = new List<string>(_pendingUiLogs);
                _pendingUiLogs.Clear();
                return logs;
            }
        }

        public void ClearPendingUiLogs()
        {
            lock (_uiSyncRoot)
            {
                _pendingUiLogs.Clear();
            }
        }

        private void EnqueueUiLog(string logEntry)
        {
            lock (_uiSyncRoot)
            {
                _pendingUiLogs.Enqueue(logEntry);

                while (_pendingUiLogs.Count > MaxPendingUiLogLines)
                {
                    _pendingUiLogs.Dequeue();
                }
            }
        }

        public void LogDebug(string message)
        {
            if(IsDebugMode)
                 Log(message);
        }

        /// <summary>
        /// 确保当前小时日志文件存在
        /// </summary>
        private void EnsureLogFile(DateTime logTime)
        {
            try
            {
                Directory.CreateDirectory(_logDirectory);
                var logFilePath = GetLogFilePath(logTime);
                if (!File.Exists(logFilePath))
                {
                    File.WriteAllText(logFilePath, $"[{logTime:yyyy-MM-dd HH}:00] 日志文件已创建{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch(Exception)
            {
                //DXMessageBox.Show("日志文件初始化失败: " + ex.Message + ex.StackTrace);
                // 可根据需要处理异常
            }
        }

        private static string ResolveLogDirectory()
        {
            var applicationLogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogDirectoryName);
            if (TryEnsureWritableDirectory(applicationLogDirectory))
            {
                return applicationLogDirectory;
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                var userLogDirectory = Path.Combine(localAppData, AppDataDirectoryName, LogDirectoryName);
                if (TryEnsureWritableDirectory(userLogDirectory))
                {
                    return userLogDirectory;
                }
            }

            var tempLogDirectory = Path.Combine(Path.GetTempPath(), AppDataDirectoryName, LogDirectoryName);
            return TryEnsureWritableDirectory(tempLogDirectory) ? tempLogDirectory : null;
        }

        private static bool TryEnsureWritableDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var testFilePath = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFilePath, string.Empty, Encoding.UTF8);
                File.Delete(testFilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清理超过最近一周的日志文件
        /// </summary>
        private void CleanupExpiredLogFiles(DateTime now)
        {
            if (_lastCleanupDate == now.Date)
            {
                return;
            }

            try
            {
                var earliestRetainedDate = now.Date.AddDays(1 - RetainedLogDays);
                foreach (var logFilePath in Directory.EnumerateFiles(_logDirectory, LogFilePrefix + "*" + LogFileExtension))
                {
                    var fileName = Path.GetFileNameWithoutExtension(logFilePath);
                    var datePart = fileName.Substring(LogFilePrefix.Length);
                    DateTime logDate;
                    if (TryParseLogFileDate(datePart, out logDate)
                        && logDate.Date < earliestRetainedDate)
                    {
                        File.Delete(logFilePath);
                    }
                }

                _lastCleanupDate = now.Date;
            }
            catch(Exception)
            {
                //DXMessageBox.Show("日志文件清理失败: " + ex.Message + ex.StackTrace);
                // 可根据需要处理异常
            }
        }

        private string GetLogFilePath(DateTime logTime)
        {
            return Path.Combine(_logDirectory, $"{LogFilePrefix}{logTime:yyyy-MM-dd-HH}{LogFileExtension}");
        }

        private static bool TryParseLogFileDate(string datePart, out DateTime logDate)
        {
            return DateTime.TryParseExact(datePart, "yyyy-MM-dd-HH", CultureInfo.InvariantCulture, DateTimeStyles.None, out logDate)
                || DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out logDate);
        }
    }
}
