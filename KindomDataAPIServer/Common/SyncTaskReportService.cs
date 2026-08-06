using Google.Protobuf;
using KindomDataAPIServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KindomDataAPIServer.Common
{
    public sealed class SyncTaskReportService
    {
        private sealed class ConfigurationRecommendation
        {
            public string OperationName { get; set; }
            public string ConfigKey { get; set; }
            public int CurrentValue { get; set; }
            public int RecommendedValue { get; set; }
            public string Reason { get; set; }
        }

        private sealed class OperationTimingSummary
        {
            public string OperationName { get; set; }
            public TimeSpan LocalReadElapsed { get; set; }
            public TimeSpan UploadWallElapsed { get; set; }
            public TimeSpan CumulativeLocalReadElapsed { get; set; }
            public int LocalReadCount { get; set; }
            public TimeSpan CumulativeUploadResponseElapsed { get; set; }
            public int UploadResponseCount { get; set; }
        }

        private const string AppDataDirectoryName = "KindomDataAPIServer";
        private const string LogDirectoryName = "Logs";
        private const string ReportDirectoryName = "TaskReports";
        private const string ReportFilePattern = "sync-task-*.txt";
        private const int MaxRecordedUploadErrorsPerOperation = 10;

        private static readonly Lazy<SyncTaskReportService> _instance =
            new Lazy<SyncTaskReportService>(() => new SyncTaskReportService());

        private readonly object _syncRoot = new object();
        private string _reportDirectory;
        private readonly Dictionary<string, int> _recordedUploadErrorCounts = new Dictionary<string, int>();
        private readonly List<ConfigurationRecommendation> _configurationRecommendations = new List<ConfigurationRecommendation>();
        private readonly List<OperationTimingSummary> _operationTimingSummaries = new List<OperationTimingSummary>();
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

        public string ReportDirectory
        {
            get
            {
                lock (_syncRoot)
                {
                    return _reportDirectory;
                }
            }
        }

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
                if (string.IsNullOrWhiteSpace(_reportDirectory) || !TryEnsureWritableDirectory(_reportDirectory))
                {
                    _reportDirectory = ResolveReportDirectory();
                }

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
                _recordedUploadErrorCounts.Clear();
                _configurationRecommendations.Clear();
                _operationTimingSummaries.Clear();
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

        public void RecordConfigurationRecommendation(
            string operationName,
            string configKey,
            int currentValue,
            int recommendedValue,
            string reason)
        {
            var recommendation = new ConfigurationRecommendation
            {
                OperationName = operationName,
                ConfigKey = configKey,
                CurrentValue = currentValue,
                RecommendedValue = recommendedValue,
                Reason = reason
            };

            lock (_syncRoot)
            {
                _configurationRecommendations.RemoveAll(item => item.OperationName == operationName);
                _configurationRecommendations.Add(recommendation);
            }

            LogSummary($"{operationName} configuration recommendation. Key:{configKey}, current:{currentValue}, recommended:{recommendedValue}, reason:{reason}");
        }

        public void RecordOperationTiming(
            string operationName,
            TimeSpan localReadElapsed,
            TimeSpan uploadWallElapsed,
            TimeSpan cumulativeLocalReadElapsed,
            int localReadCount,
            TimeSpan cumulativeUploadResponseElapsed,
            int uploadResponseCount)
        {
            var timingSummary = new OperationTimingSummary
            {
                OperationName = operationName,
                LocalReadElapsed = localReadElapsed,
                UploadWallElapsed = uploadWallElapsed,
                CumulativeLocalReadElapsed = cumulativeLocalReadElapsed,
                LocalReadCount = localReadCount,
                CumulativeUploadResponseElapsed = cumulativeUploadResponseElapsed,
                UploadResponseCount = uploadResponseCount
            };

            lock (_syncRoot)
            {
                _operationTimingSummaries.RemoveAll(item => item.OperationName == operationName);
                _operationTimingSummaries.Add(timingSummary);
            }
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
            string status = completedSuccessfully ? "Succeeded" : "Completed";
            List<ConfigurationRecommendation> recommendations;
            List<OperationTimingSummary> operationTimings;
            WriteReportLine(new string('-', 48));
            lock (_syncRoot)
            {
                WriteReportLine($"Overall API reads: {_apiReadCount}");
                WriteReportLine($"Overall upload requests: {_uploadRequestCount}");
                WriteReportLine($"Overall upload payload bytes: {_totalUploadBytes} ({_totalUploadBytes / 1024.0 / 1024.0:F3} MiB)");
                WriteReportLine($"Overall API read elapsed: {TimeSpan.FromTicks(_apiReadElapsedTicks).TotalSeconds:F3}s");
                WriteReportLine($"Cumulative upload request elapsed: {TimeSpan.FromTicks(_uploadElapsedTicks).TotalSeconds:F3}s");
                WriteReportLine($"Overall synchronization errors: {_errorCount}");
                recommendations = _configurationRecommendations.ToList();
                operationTimings = _operationTimingSummaries.ToList();
            }
            foreach (OperationTimingSummary operationTiming in operationTimings)
            {
                double measuredSeconds = operationTiming.LocalReadElapsed.TotalSeconds
                    + operationTiming.UploadWallElapsed.TotalSeconds;
                double localReadShare = measuredSeconds <= 0
                    ? 0
                    : operationTiming.LocalReadElapsed.TotalSeconds * 100.0 / measuredSeconds;
                double uploadShare = measuredSeconds <= 0
                    ? 0
                    : operationTiming.UploadWallElapsed.TotalSeconds * 100.0 / measuredSeconds;
                double averageUploadResponseSeconds = operationTiming.UploadResponseCount <= 0
                    ? 0
                    : operationTiming.CumulativeUploadResponseElapsed.TotalSeconds / operationTiming.UploadResponseCount;
                double averageLocalReadSeconds = operationTiming.LocalReadCount <= 0
                    ? 0
                    : operationTiming.CumulativeLocalReadElapsed.TotalSeconds / operationTiming.LocalReadCount;
                WriteReportLine($"{operationTiming.OperationName} actual timing: local read elapsed:{operationTiming.LocalReadElapsed.TotalSeconds:F3}s, upload wall elapsed:{operationTiming.UploadWallElapsed.TotalSeconds:F3}s, local read time share:{localReadShare:F1}%, upload time share:{uploadShare:F1}%, average upload API response elapsed:{averageUploadResponseSeconds:F3}s, average local read elapsed:{averageLocalReadSeconds:F3}s.");
            }
            if (recommendations.Count > 0)
            {
                var recommendationBlock = new StringBuilder();
                recommendationBlock.AppendLine("Recommended appSettings for the next run (keep all unlisted settings unchanged):");
                foreach (ConfigurationRecommendation recommendation in recommendations)
                {
                    recommendationBlock.AppendLine($"<!-- {recommendation.OperationName}: {recommendation.Reason} -->");
                    recommendationBlock.AppendLine($"<add key=\"{recommendation.ConfigKey}\" value=\"{recommendation.RecommendedValue}\" />");
                }
                WriteReportLine(recommendationBlock.ToString().TrimEnd());
            }
            WriteReportLine($"Task status: {status}");
            WriteReportLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteReportLine("Total task elapsed: " + elapsed.ToString(@"hh\:mm\:ss\.fff"));
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                WriteReportLine($"Error: {errorMessage}");
            }

            string reportPath = CurrentReportPath;
            LogManagerService.Instance.Log($"Sync task report completed. Status:{status}, path:{reportPath ?? "unavailable"}");
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

        public void RecordUploadResponseErrors(
            string operationName,
            string errorDirectoryName,
            string endpoint,
            string batchUwis,
            string failedUwis,
            int batchIndex,
            long payloadBytes,
            object request,
            WellOperationResult response,
            string additionalRequestParameters = null)
        {
            int failedCount = response?.Summary?.failed
                ?? response?.Results?.Count(result => result.errorCode != 0)
                ?? 0;
            if (failedCount <= 0 || !TryReserveUploadError(operationName))
            {
                return;
            }

            WellOperationDetail firstFailedItem = response.Results?
                .FirstOrDefault(result => result.errorCode != 0);
            string detailsPath = WriteUploadErrorDetails(
                operationName,
                errorDirectoryName,
                endpoint,
                batchUwis,
                batchIndex,
                payloadBytes,
                request,
                response,
                additionalRequestParameters);
            RecordError($"{operationName} batch {batchIndex} synchronization failed. Failed:{failedCount}, UWIs:[{failedUwis ?? batchUwis}], first errorCode:{firstFailedItem?.errorCode}, first errorMessage:{firstFailedItem?.Message}, request details:{detailsPath ?? "unavailable"}");
        }

        public void RecordDataValidationError(
            string operationName,
            string errorDirectoryName,
            string uwi,
            string itemDescription,
            string validationMessage,
            object sourceData)
        {
            string detailsPath = null;
            if (TryReserveUploadError(operationName + "DataValidation"))
            {
                detailsPath = WriteDataValidationErrorDetails(
                    operationName,
                    errorDirectoryName,
                    uwi,
                    itemDescription,
                    validationMessage,
                    sourceData);
            }

            RecordError($"{operationName} synchronization skipped for UWI:{uwi}, {itemDescription} due to {validationMessage}. Source data details:{detailsPath ?? "unavailable"}");
        }

        private string WriteDataValidationErrorDetails(
            string operationName,
            string errorDirectoryName,
            string uwi,
            string itemDescription,
            string validationMessage,
            object sourceData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_reportDirectory))
                {
                    LogManagerService.Instance.Log($"Failed to write {operationName} data validation error details because no writable log directory is available.");
                    return null;
                }

                string logDirectory = Directory.GetParent(_reportDirectory)?.FullName;
                if (string.IsNullOrWhiteSpace(logDirectory))
                {
                    LogManagerService.Instance.Log($"Failed to resolve the log directory for {operationName} data validation error details.");
                    return null;
                }

                string errorDirectory = Path.Combine(logDirectory, SanitizeFileName(errorDirectoryName));
                Directory.CreateDirectory(errorDirectory);
                string fileNamePrefix = SanitizeFileName(uwi);
                string filePath = Path.Combine(errorDirectory, $"{fileNamePrefix}-{Guid.NewGuid():N}.txt");

                var details = new StringBuilder();
                details.AppendLine(operationName + " Data Validation Error Details");
                details.AppendLine(new string('=', 48));
                details.AppendLine($"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                details.AppendLine($"UWI: {uwi ?? string.Empty}");
                details.AppendLine($"Item: {itemDescription ?? string.Empty}");
                details.AppendLine($"Validation error: {validationMessage ?? string.Empty}");
                details.AppendLine(new string('-', 48));
                details.AppendLine("Source data JSON:");
                details.AppendLine(JsonHelper.ToJson(sourceData));
                File.WriteAllText(filePath, details.ToString(), Encoding.UTF8);
                return filePath;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"Failed to write {operationName} data validation error details: " + ExceptionLogHelper.Format(ex));
                return null;
            }
        }

        private string WriteUploadErrorDetails(
            string operationName,
            string errorDirectoryName,
            string endpoint,
            string batchUwis,
            int batchIndex,
            long payloadBytes,
            object request,
            object response,
            string additionalRequestParameters)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_reportDirectory))
                {
                    LogManagerService.Instance.Log($"Failed to write {operationName} upload error details because no writable log directory is available.");
                    return null;
                }

                string logDirectory = Directory.GetParent(_reportDirectory)?.FullName;
                if (string.IsNullOrWhiteSpace(logDirectory))
                {
                    LogManagerService.Instance.Log($"Failed to resolve the log directory for {operationName} upload error details.");
                    return null;
                }

                string errorDirectory = Path.Combine(logDirectory, SanitizeFileName(errorDirectoryName));
                Directory.CreateDirectory(errorDirectory);
                string fileNamePrefix = SanitizeFileName(batchUwis);
                string filePath = Path.Combine(errorDirectory, $"{fileNamePrefix}-{Guid.NewGuid():N}.txt");
                IMessage protobufRequest = request as IMessage;
                string requestFormat = protobufRequest == null
                    ? "Request JSON"
                    : "Request Protobuf JSON representation";
                string requestContent = protobufRequest == null
                    ? JsonHelper.ToJson(request)
                    : JsonFormatter.Default.Format(protobufRequest);

                var details = new StringBuilder();
                details.AppendLine(operationName + " Upload Error Details");
                details.AppendLine(new string('=', 48));
                details.AppendLine($"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                details.AppendLine($"Endpoint: {endpoint}");
                details.AppendLine($"Batch: {batchIndex}");
                details.AppendLine($"UWIs: {batchUwis ?? string.Empty}");
                details.AppendLine($"Payload bytes: {payloadBytes}");
                if (!string.IsNullOrWhiteSpace(additionalRequestParameters))
                {
                    details.AppendLine("Additional request parameters:");
                    details.AppendLine(additionalRequestParameters);
                }
                details.AppendLine(new string('-', 48));
                details.AppendLine(requestFormat + ":");
                details.AppendLine(requestContent);
                details.AppendLine(new string('-', 48));
                details.AppendLine("Response JSON:");
                details.AppendLine(JsonHelper.ToJson(response));
                File.WriteAllText(filePath, details.ToString(), Encoding.UTF8);
                return filePath;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"Failed to write {operationName} upload error details: " + ExceptionLogHelper.Format(ex));
                return null;
            }
        }

        private bool TryReserveUploadError(string operationName)
        {
            lock (_syncRoot)
            {
                string key = operationName ?? string.Empty;
                _recordedUploadErrorCounts.TryGetValue(key, out int currentCount);
                if (currentCount >= MaxRecordedUploadErrorsPerOperation)
                {
                    return false;
                }

                _recordedUploadErrorCounts[key] = currentCount + 1;
                return true;
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
