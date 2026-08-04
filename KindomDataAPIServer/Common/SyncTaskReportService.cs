using System;
using System.IO;
using System.Linq;
using System.Text;

namespace KindomDataAPIServer.Common
{
    public sealed class SyncTaskReportService
    {
        private const string AppDataDirectoryName = "KindomDataAPIServer";
        private const string LogDirectoryName = "Logs";
        private const string ReportDirectoryName = "TaskReports";
        private const string ReportFilePattern = "sync-task-*.txt";

        private static readonly Lazy<SyncTaskReportService> _instance =
            new Lazy<SyncTaskReportService>(() => new SyncTaskReportService());

        private readonly object _syncRoot = new object();
        private readonly string _reportDirectory;
        private string _currentReportPath;
        private long _apiReadCount;
        private long _uploadRequestCount;
        private long _totalUploadBytes;
        private long _apiReadElapsedTicks;
        private long _uploadElapsedTicks;
        private long _errorCount;

        private SyncTaskReportService()
        {
            _reportDirectory = ResolveReportDirectory();
        }

        public static SyncTaskReportService Instance => _instance.Value;

        public string ReportDirectory => _reportDirectory;

        public long ErrorCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _errorCount;
                }
            }
        }

        public string CurrentReportPath
        {
            get
            {
                lock (_syncRoot)
                {
                    return _currentReportPath;
                }
            }
        }

        public string BeginTask(string projectPath, string authorName)
        {
            lock (_syncRoot)
            {
                if (string.IsNullOrWhiteSpace(_reportDirectory))
                {
                    _currentReportPath = null;
                    LogManagerService.Instance.Log("Sync task report is disabled because no writable report directory is available.");
                    return null;
                }

                DateTime now = DateTime.Now;
                _apiReadCount = 0;
                _uploadRequestCount = 0;
                _totalUploadBytes = 0;
                _apiReadElapsedTicks = 0;
                _uploadElapsedTicks = 0;
                _errorCount = 0;
                _currentReportPath = Path.Combine(
                    _reportDirectory,
                    $"sync-task-{now:yyyyMMdd-HHmmss-fff}.txt");

                var header = new StringBuilder();
                header.AppendLine("Kingdom Data Synchronization Task Report");
                header.AppendLine(new string('=', 48));
                header.AppendLine($"Started: {now:yyyy-MM-dd HH:mm:ss.fff}");
                header.AppendLine($"Project: {projectPath ?? string.Empty}");
                header.AppendLine($"Author: {authorName ?? string.Empty}");
                header.AppendLine($"Report: {_currentReportPath}");
                header.AppendLine(new string('-', 48));
                try
                {
                    File.WriteAllText(_currentReportPath, header.ToString(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    _currentReportPath = null;
                    LogManagerService.Instance.Log("Failed to create sync task report: " + ExceptionLogHelper.Format(ex));
                    return null;
                }

                LogManagerService.Instance.Log($"Sync task report started: {_currentReportPath}");
                return _currentReportPath;
            }
        }

        public void Log(string message)
        {
            LogManagerService.Instance.Log(message);
        }

        public void LogSummary(string message)
        {
            LogManagerService.Instance.Log(message);
            WriteReportLine(message);
        }

        public void RecordError(string context, Exception exception = null)
        {
            string message = exception == null
                ? context
                : context + ": " + ExceptionLogHelper.Format(exception);
            lock (_syncRoot)
            {
                _errorCount++;
            }
            LogSummary(message);
        }

        public void Complete(bool succeeded, TimeSpan elapsed, string errorMessage = null)
        {
            bool completedSuccessfully = succeeded && ErrorCount == 0;
            string status = completedSuccessfully ? "Succeeded" : "Failed";
            WriteReportLine(new string('-', 48));
            lock (_syncRoot)
            {
                WriteReportLine($"Overall API reads: {_apiReadCount}");
                WriteReportLine($"Overall upload requests: {_uploadRequestCount}");
                WriteReportLine($"Overall upload payload bytes: {_totalUploadBytes} ({_totalUploadBytes / 1024.0 / 1024.0:F3} MiB)");
                WriteReportLine($"Overall API read elapsed: {TimeSpan.FromTicks(_apiReadElapsedTicks).TotalSeconds:F3}s");
                WriteReportLine($"Cumulative upload request elapsed: {TimeSpan.FromTicks(_uploadElapsedTicks).TotalSeconds:F3}s");
                WriteReportLine($"Overall synchronization errors: {_errorCount}");
            }
            WriteReportLine($"Task status: {status}");
            WriteReportLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteReportLine("Total task elapsed: " + elapsed.ToString(@"hh\:mm\:ss\.fff"));
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                WriteReportLine($"Error: {errorMessage}");
            }

            LogManagerService.Instance.Log($"Sync task report completed. Status:{status}, path:{CurrentReportPath}");
            DisplayCurrentReportInApplicationLog();
        }

        public void RecordApiRead(TimeSpan elapsed)
        {
            lock (_syncRoot)
            {
                _apiReadCount++;
                _apiReadElapsedTicks += elapsed.Ticks;
            }
        }

        public void RecordUploadAttempt(long payloadBytes)
        {
            lock (_syncRoot)
            {
                _uploadRequestCount++;
                _totalUploadBytes += payloadBytes;
            }
        }

        public void RecordUploadElapsed(TimeSpan elapsed)
        {
            lock (_syncRoot)
            {
                _uploadElapsedTicks += elapsed.Ticks;
            }
        }

        public string GetLatestReportPath()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_reportDirectory) || !Directory.Exists(_reportDirectory))
                {
                    return null;
                }

                return Directory.EnumerateFiles(_reportDirectory, ReportFilePattern)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private void WriteReportLine(string message)
        {
            lock (_syncRoot)
            {
                if (string.IsNullOrWhiteSpace(_currentReportPath))
                {
                    return;
                }

                try
                {
                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}";
                    File.AppendAllText(_currentReportPath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    LogManagerService.Instance.Log("Failed to write sync task report: " + ExceptionLogHelper.Format(ex));
                }
            }
        }

        private void DisplayCurrentReportInApplicationLog()
        {
            string reportPath = CurrentReportPath;
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                return;
            }

            try
            {
                string reportContent = File.ReadAllText(reportPath, Encoding.UTF8).TrimEnd();
                if (!string.IsNullOrWhiteSpace(reportContent))
                {
                    LogManagerService.Instance.Log("Task report:" + Environment.NewLine + reportContent);
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("Failed to display sync task report in application log: " + ExceptionLogHelper.Format(ex));
            }
        }

        private static string ResolveReportDirectory()
        {
            string applicationDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                LogDirectoryName,
                ReportDirectoryName);
            if (TryEnsureWritableDirectory(applicationDirectory))
            {
                return applicationDirectory;
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                string userDirectory = Path.Combine(
                    localAppData,
                    AppDataDirectoryName,
                    LogDirectoryName,
                    ReportDirectoryName);
                if (TryEnsureWritableDirectory(userDirectory))
                {
                    return userDirectory;
                }
            }

            string tempDirectory = Path.Combine(
                Path.GetTempPath(),
                AppDataDirectoryName,
                LogDirectoryName,
                ReportDirectoryName);
            return TryEnsureWritableDirectory(tempDirectory) ? tempDirectory : null;
        }

        private static bool TryEnsureWritableDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                string testFilePath = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFilePath, string.Empty, Encoding.UTF8);
                File.Delete(testFilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
