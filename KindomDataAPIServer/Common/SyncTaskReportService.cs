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
        private const string WellTrajectoryErrorDirectoryName = "WellTrajectoryUploadErrors";
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

        public string WriteWellTrajectoryUploadErrorDetails(
            string batchUwis,
            int batchIndex,
            long payloadBytes,
            object request,
            object response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_reportDirectory))
                {
                    LogManagerService.Instance.Log("Failed to write well trajectory upload error details because no writable log directory is available.");
                    return null;
                }

                string logDirectory = Directory.GetParent(_reportDirectory)?.FullName;
                if (string.IsNullOrWhiteSpace(logDirectory))
                {
                    LogManagerService.Instance.Log("Failed to resolve the log directory for well trajectory upload error details.");
                    return null;
                }

                string errorDirectory = Path.Combine(logDirectory, WellTrajectoryErrorDirectoryName);
                Directory.CreateDirectory(errorDirectory);
                string fileNamePrefix = SanitizeFileName(batchUwis);
                string filePath = Path.Combine(errorDirectory, $"{fileNamePrefix}-{Guid.NewGuid():N}.txt");

                var details = new StringBuilder();
                details.AppendLine("Well Trajectory Upload Error Details");
                details.AppendLine(new string('=', 48));
                details.AppendLine($"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                details.AppendLine("Endpoint: dp/api/welldata/batch_create_well_trajectory_with_meta_infos");
                details.AppendLine($"Batch: {batchIndex}");
                details.AppendLine($"UWIs: {batchUwis ?? string.Empty}");
                details.AppendLine($"JSON payload bytes: {payloadBytes}");
                details.AppendLine(new string('-', 48));
                details.AppendLine("Request JSON:");
                details.AppendLine(JsonHelper.ToJson(request));
                details.AppendLine(new string('-', 48));
                details.AppendLine("Response JSON:");
                details.AppendLine(JsonHelper.ToJson(response));
                File.WriteAllText(filePath, details.ToString(), Encoding.UTF8);
                return filePath;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("Failed to write well trajectory upload error details: " + ExceptionLogHelper.Format(ex));
                return null;
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

        private static string SanitizeFileName(string value)
        {
            string fileName = string.IsNullOrWhiteSpace(value) ? "unknown-uwi" : value;
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            fileName = new string(fileName
                .Select(character => invalidCharacters.Contains(character) ? '_' : character)
                .ToArray())
                .Trim();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "unknown-uwi";
            }

            const int maxPrefixLength = 120;
            return fileName.Length <= maxPrefixLength
                ? fileName
                : fileName.Substring(0, maxPrefixLength);
        }
    }
}
