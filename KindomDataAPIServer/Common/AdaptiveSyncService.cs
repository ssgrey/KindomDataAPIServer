using KindomDataAPIServer.DataService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Common
{
    public sealed class AdaptiveSyncSettings
    {
        public int Version { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public AdaptiveCommonSettings Common { get; set; } = new AdaptiveCommonSettings();
        public AdaptiveFixedSettings Fixed { get; set; } = new AdaptiveFixedSettings();
        public AdaptiveDataTypesSettings DataTypes { get; set; } = new AdaptiveDataTypesSettings();
    }

    public sealed class AdaptiveCommonSettings
    {
        public int FastPayloadGrowthPercent { get; set; } = 75;
        public int StablePayloadGrowthPercent { get; set; } = 25;
        public int ThroughputImprovementPercent { get; set; } = 10;
        public int RollbackAfterNoImprovementWindows { get; set; } = 2;
        public int CooldownWindows { get; set; } = 2;
        public bool AllowConcurrencyIncrease { get; set; }
    }

    public sealed class AdaptiveFixedSettings
    {
        public int WellHeaderUploadBatchSize { get; set; } = 5000;
        public bool UseFileImport { get; set; }
    }

    public sealed class AdaptiveDataTypesSettings
    {
        public AdaptiveDataTypeSettings Formation { get; set; } = AdaptiveDataTypeSettings.CreateFormation();
        public AdaptiveDataTypeSettings Trajectory { get; set; } = AdaptiveDataTypeSettings.CreateTrajectory();
        public AdaptiveDataTypeSettings Production { get; set; } = AdaptiveDataTypeSettings.CreateProduction();
        public AdaptiveDataTypeSettings WellLog { get; set; } = AdaptiveDataTypeSettings.CreateWellLog();
    }

    public sealed class AdaptiveDataTypeSettings
    {
        public AdaptiveReadBatchSettings ReadBatch { get; set; } = new AdaptiveReadBatchSettings();
        public AdaptiveUploadSettings Upload { get; set; } = new AdaptiveUploadSettings();

        public static AdaptiveDataTypeSettings CreateFormation()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 20, Max = 100, TargetSeconds = 3 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 1, MaxPayloadMiB = 64, Concurrency = 2, StableEvaluationRequests = 4, MaxFormationCount = 100000 }
            };
        }

        public static AdaptiveDataTypeSettings CreateTrajectory()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 5, Max = 100, TargetSeconds = 2 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 8, MaxPayloadMiB = 64, Concurrency = 3, StableEvaluationRequests = 6, MaxPointCount = 2000000 }
            };
        }

        public static AdaptiveDataTypeSettings CreateProduction()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 2, Max = 50, TargetSeconds = 3 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 4, MaxPayloadMiB = 64, Concurrency = 3, StableEvaluationRequests = 6, MaxDailyDataCount = 1000000 }
            };
        }

        public static AdaptiveDataTypeSettings CreateWellLog()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 3, Max = 20, TargetSeconds = 3 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 4, MaxPayloadMiB = 64, Concurrency = 5, StableEvaluationRequests = 10, MaxSampleCount = 5000000 }
            };
        }
    }

    public sealed class AdaptiveReadBatchSettings
    {
        public int Initial { get; set; } = 1;
        public int Max { get; set; } = 1;
        public double TargetSeconds { get; set; } = 3;
    }

    public sealed class AdaptiveUploadSettings
    {
        public double InitialPayloadMiB { get; set; } = 0.5;
        public double MaxPayloadMiB { get; set; } = 2;
        public int Concurrency { get; set; } = 1;
        public int StableEvaluationRequests { get; set; } = 3;
        public int MaxFormationCount { get; set; } = 1000;
        public int MaxPointCount { get; set; } = 20000;
        public int MaxDailyDataCount { get; set; } = 2000;
        public int MaxSampleCount { get; set; } = 1000000;
    }

    public sealed class AdaptiveSyncState
    {
        public int Version { get; set; } = 1;
        public DateTime UpdatedAtUtc { get; set; }
        public Dictionary<string, AdaptiveDataTypeState> DataTypes { get; set; } =
            new Dictionary<string, AdaptiveDataTypeState>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class AdaptiveDataTypeState
    {
        public long BestStablePayloadBytes { get; set; }
        public int BestStableReadBatch { get; set; }
        public double BestStableThroughputBytesPerSecond { get; set; }
        public int ValidEvaluationWindows { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string EnvironmentId { get; set; }
    }

    public sealed class AdaptiveSyncSession
    {
        private readonly Dictionary<string, AdaptiveSyncController> _controllers;
        private readonly AdaptiveSyncState _state;
        private readonly string _environmentId;
        private bool _saved;

        internal AdaptiveSyncSession(
            AdaptiveSyncSettings settings,
            AdaptiveSyncState state,
            string environmentId,
            string settingsPath,
            bool settingsLoaded,
            string stateLoadPath)
        {
            Settings = settings;
            _state = state ?? new AdaptiveSyncState();
            _environmentId = environmentId ?? string.Empty;
            SettingsPath = settingsPath;
            SettingsLoaded = settingsLoaded;
            StateLoadPath = stateLoadPath;
            _controllers = new Dictionary<string, AdaptiveSyncController>(StringComparer.OrdinalIgnoreCase);
            AddController("formation", settings.DataTypes.Formation);
            AddController("trajectory", settings.DataTypes.Trajectory);
            AddController("production", settings.DataTypes.Production);
            AddController("wellLog", settings.DataTypes.WellLog);
        }

        public AdaptiveSyncSettings Settings { get; }
        public string SettingsPath { get; }
        public bool SettingsLoaded { get; }
        public string StateLoadPath { get; }
        public string StateSavePath { get; private set; }
        public bool StateSaveSucceeded { get; private set; }

        public AdaptiveSyncController GetController(string dataType)
        {
            return _controllers[dataType];
        }

        public void Save()
        {
            if (_saved)
            {
                return;
            }
            _saved = true;

            foreach (var item in _controllers)
            {
                try
                {
                    AdaptiveDataTypeState learnedState = item.Value.CreateLearnedState(_environmentId);
                    if (learnedState != null)
                    {
                        _state.DataTypes[item.Key] = learnedState;
                    }
                }
                catch (Exception ex)
                {
                    SyncTaskReportService.Instance.LogSummary($"Failed to update adaptive sync state for {item.Key}; other data types will still be saved. {ExceptionLogHelper.Format(ex)}");
                }
            }

            _state.UpdatedAtUtc = DateTime.UtcNow;
            StateSaveSucceeded = AdaptiveSyncService.TrySaveState(_state, out string savePath);
            StateSavePath = savePath;
            string message = $"Adaptive sync state save. Success:{StateSaveSucceeded}, path:{StateSavePath ?? "unavailable"}.";
            SyncTaskReportService.Instance.LogSummary(message);
        }

        private void AddController(string name, AdaptiveDataTypeSettings typeSettings)
        {
            _state.DataTypes.TryGetValue(name, out AdaptiveDataTypeState learned);
            if (learned != null && !string.Equals(learned.EnvironmentId, _environmentId, StringComparison.OrdinalIgnoreCase))
            {
                learned = null;
            }
            _controllers[name] = new AdaptiveSyncController(name, Settings.Enabled, Settings.Common, typeSettings, learned);
        }
    }

    public sealed class AdaptiveSyncController
    {
        private const long OneMiB = 1024L * 1024L;
        private const long MinimumPayloadBytes = 64L * 1024L;
        private const long MemoryPressureBytes = 1536L * OneMiB;
        private readonly object _syncRoot = new object();
        private readonly string _dataType;
        private readonly bool _enabled;
        private readonly AdaptiveCommonSettings _common;
        private readonly AdaptiveDataTypeSettings _settings;
        private readonly int _configuredReadInitial;
        private readonly long _configuredPayloadInitial;
        private readonly int _actualInitialReadBatch;
        private readonly long _actualInitialPayloadBytes;
        private readonly long _maximumPayloadBytes;
        private readonly AdaptiveDataTypeState _learned;
        private int _currentReadBatch;
        private long _currentPayloadBytes;
        private int _effectiveConcurrency;
        private int _activeUploads;
        private int _stableReadWindows;
        private bool _fastPhase = true;
        private int _cooldownWindows;
        private int _noImprovementWindows;
        private int _windowRequestCount;
        private long _windowPayloadBytes;
        private DateTime _windowStartedUtc;
        private double _baselineThroughput;
        private double _bestThroughput;
        private double _recentRequestLatencySeconds;
        private long _bestPayloadBytes;
        private int _bestReadBatch;
        private int _validWindows;
        private bool _completedSuccessfully;
        private bool _transportOrInternalFailure;
        private bool _lastAdjustmentWasProtection;
        private int _payloadGrowthCount;
        private int _payloadRollbackCount;
        private int _readGrowthCount;
        private int _readRollbackCount;
        private int _httpProtectionCount;
        private int _concurrencyReductionCount;
        private int _oversizedItemCount;
        private long _preparationTicks;
        private long _serializationTicks;
        private long _queueWaitTicks;
        private int _minimumReadBatch;
        private int _maximumReadBatch;
        private long _minimumPayloadBytes;
        private long _maximumObservedPayloadBytes;
        private int _minimumConcurrency;
        private int _lastQueueOccupancy;

        internal AdaptiveSyncController(
            string dataType,
            bool enabled,
            AdaptiveCommonSettings common,
            AdaptiveDataTypeSettings settings,
            AdaptiveDataTypeState learned)
        {
            _dataType = dataType;
            _enabled = enabled;
            _common = common;
            _settings = settings;
            _learned = learned;
            _configuredReadInitial = settings.ReadBatch.Initial;
            _configuredPayloadInitial = ToBytes(settings.Upload.InitialPayloadMiB);
            _maximumPayloadBytes = ToBytes(settings.Upload.MaxPayloadMiB);
            _currentReadBatch = enabled && learned != null && learned.BestStableReadBatch > 0
                ? Clamp((int)Math.Round(learned.BestStableReadBatch * 0.75), 1, settings.ReadBatch.Max)
                : settings.ReadBatch.Initial;
            _currentPayloadBytes = enabled && learned != null && learned.BestStablePayloadBytes > 0
                ? Clamp(learned.BestStablePayloadBytes, MinimumPayloadBytes, _maximumPayloadBytes)
                : _configuredPayloadInitial;
            _actualInitialReadBatch = _currentReadBatch;
            _actualInitialPayloadBytes = _currentPayloadBytes;
            _effectiveConcurrency = settings.Upload.Concurrency;
            _bestPayloadBytes = _currentPayloadBytes;
            _bestReadBatch = _currentReadBatch;
            _bestThroughput = learned?.BestStableThroughputBytesPerSecond ?? 0;
            _minimumReadBatch = _maximumReadBatch = _currentReadBatch;
            _minimumPayloadBytes = _maximumObservedPayloadBytes = _currentPayloadBytes;
            _minimumConcurrency = _effectiveConcurrency;

            SyncTaskReportService.Instance.LogSummary(
                $"Adaptive sync {_dataType} initialized. Enabled:{_enabled}, configured read initial/max:{_configuredReadInitial}/{settings.ReadBatch.Max}, learned read:{learned?.BestStableReadBatch ?? 0}, actual read initial:{_currentReadBatch}, configured payload initial/max:{_configuredPayloadInitial}/{_maximumPayloadBytes} bytes, learned payload:{learned?.BestStablePayloadBytes ?? 0}, actual payload initial:{_currentPayloadBytes} bytes, concurrency:{settings.Upload.Concurrency}.");
        }

        public int CurrentReadBatch { get { lock (_syncRoot) { return _currentReadBatch; } } }
        public long CurrentPayloadBytes { get { lock (_syncRoot) { return _currentPayloadBytes; } } }
        public int ConfiguredConcurrency => _settings.Upload.Concurrency;
        public int CurrentQueueCapacity { get { lock (_syncRoot) { return _enabled && _fastPhase ? _settings.Upload.Concurrency : _settings.Upload.Concurrency * 2; } } }
        public int BusinessLimit
        {
            get
            {
                switch (_dataType.ToLowerInvariant())
                {
                    case "formation": return _settings.Upload.MaxFormationCount;
                    case "trajectory": return _settings.Upload.MaxPointCount;
                    case "production": return _settings.Upload.MaxDailyDataCount;
                    default: return _settings.Upload.MaxSampleCount;
                }
            }
        }

        public bool HasMemoryPressure()
        {
            try
            {
                return Process.GetCurrentProcess().PrivateMemorySize64 >= MemoryPressureBytes;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IDisposable> EnterUploadAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                lock (_syncRoot)
                {
                    if (_activeUploads < _effectiveConcurrency)
                    {
                        _activeUploads++;
                        return new UploadLease(this);
                    }
                }
                await Task.Delay(50, cancellationToken);
            }
        }

        public void RecordRead(TimeSpan elapsed, long dataCount, bool queueWasFull)
        {
            try
            {
                lock (_syncRoot)
                {
                    bool pressure = HasMemoryPressure();
                    if (!_enabled)
                    {
                        return;
                    }
                    if (elapsed.TotalSeconds > _settings.ReadBatch.TargetSeconds || dataCount > BusinessLimit || pressure)
                    {
                        int oldValue = _currentReadBatch;
                        _currentReadBatch = Math.Max(1, _currentReadBatch / 2);
                        _stableReadWindows = 0;
                        if (_currentReadBatch != oldValue)
                        {
                            _readRollbackCount++;
                            LogAdjustment("protect", "read batch", oldValue, _currentReadBatch,
                                $"read elapsed:{elapsed.TotalSeconds:F3}s, data count:{dataCount}, queue full:{queueWasFull}, memory pressure:{pressure}");
                        }
                    }
                    else if (!queueWasFull)
                    {
                        _stableReadWindows++;
                        if (_stableReadWindows >= 2 && _currentReadBatch < _settings.ReadBatch.Max)
                        {
                            int oldValue = _currentReadBatch;
                            _currentReadBatch = Math.Min(_settings.ReadBatch.Max,
                                Math.Max(oldValue + 1, (int)Math.Ceiling(oldValue * 1.25)));
                            _stableReadWindows = 0;
                            _readGrowthCount++;
                            _bestReadBatch = Math.Max(_bestReadBatch, oldValue);
                            LogAdjustment("stable", "read batch", oldValue, _currentReadBatch, "two stable read batches");
                        }
                    }
                    _minimumReadBatch = Math.Min(_minimumReadBatch, _currentReadBatch);
                    _maximumReadBatch = Math.Max(_maximumReadBatch, _currentReadBatch);
                }
            }
            catch (Exception ex)
            {
                ResetToInitial("read controller failure", ex);
            }
        }

        public void RecordUpload(long payloadBytes, ApiRequestTelemetry telemetry, bool transportSucceeded)
        {
            try
            {
                lock (_syncRoot)
                {
                    if (!transportSucceeded)
                    {
                        _transportOrInternalFailure = true;
                    }
                    if (!_enabled)
                    {
                        return;
                    }
                    if (HasMemoryPressure())
                    {
                        ApplyProtection("memory-pressure");
                        return;
                    }
                    if (!transportSucceeded && telemetry == null)
                    {
                        ApplyProtection("request-failure");
                        return;
                    }
                    if (telemetry != null && telemetry.HasProtectionSignal)
                    {
                        ApplyProtection(telemetry.Signal);
                        return;
                    }
                    if (!transportSucceeded)
                    {
                        return;
                    }

                    if (telemetry != null && telemetry.TotalElapsed.TotalSeconds > 0)
                    {
                        double currentLatency = telemetry.TotalElapsed.TotalSeconds;
                        if (_recentRequestLatencySeconds > 0 && currentLatency > _recentRequestLatencySeconds * 2)
                        {
                            _recentRequestLatencySeconds = currentLatency;
                            ApplyProtection("latency-regression");
                            return;
                        }
                        _recentRequestLatencySeconds = _recentRequestLatencySeconds <= 0
                            ? currentLatency
                            : _recentRequestLatencySeconds * 0.75 + currentLatency * 0.25;
                    }

                    DateTime now = DateTime.UtcNow;
                    if (_windowRequestCount == 0)
                    {
                        _windowStartedUtc = telemetry != null && telemetry.TotalElapsed > TimeSpan.Zero
                            ? now - telemetry.TotalElapsed
                            : now;
                    }
                    _windowRequestCount++;
                    _windowPayloadBytes += payloadBytes;
                    int requiredRequests = _fastPhase ? _settings.Upload.Concurrency : _settings.Upload.StableEvaluationRequests;
                    if (_windowRequestCount < requiredRequests)
                    {
                        return;
                    }

                    double seconds = Math.Max(0.001, (now - _windowStartedUtc).TotalSeconds);
                    double throughput = _windowPayloadBytes / seconds;
                    int requestCount = _windowRequestCount;
                    long windowBytes = _windowPayloadBytes;
                    _windowRequestCount = 0;
                    _windowPayloadBytes = 0;
                    _validWindows++;

                    if (_baselineThroughput <= 0)
                    {
                        _baselineThroughput = throughput;
                    }
                    double improvement = _baselineThroughput <= 0 ? 0 : (throughput / _baselineThroughput - 1) * 100;
                    if (throughput > _bestThroughput)
                    {
                        _bestThroughput = throughput;
                        _bestPayloadBytes = _currentPayloadBytes;
                        _bestReadBatch = _currentReadBatch;
                    }

                    if (_cooldownWindows > 0)
                    {
                        _cooldownWindows--;
                        if (_cooldownWindows == 0)
                        {
                            _lastAdjustmentWasProtection = false;
                        }
                        LogWindow("cooldown", requestCount, windowBytes, seconds, throughput, improvement, "growth suppressed");
                        return;
                    }

                    if (_fastPhase)
                    {
                        if (_currentPayloadBytes >= _maximumPayloadBytes || (_validWindows > 1 && improvement < _common.ThroughputImprovementPercent))
                        {
                            _fastPhase = false;
                            _baselineThroughput = Math.Max(_baselineThroughput, throughput);
                            LogWindow("fast", requestCount, windowBytes, seconds, throughput, improvement, "entered stable phase");
                        }
                        else
                        {
                            GrowPayload(_common.FastPayloadGrowthPercent, "fast", requestCount, windowBytes, seconds, throughput, improvement);
                        }
                        return;
                    }

                    if (improvement >= _common.ThroughputImprovementPercent)
                    {
                        _noImprovementWindows = 0;
                        _baselineThroughput = throughput;
                        GrowPayload(_common.StablePayloadGrowthPercent, "stable", requestCount, windowBytes, seconds, throughput, improvement);
                    }
                    else
                    {
                        _noImprovementWindows++;
                        if (_noImprovementWindows >= _common.RollbackAfterNoImprovementWindows && _bestPayloadBytes < _currentPayloadBytes)
                        {
                            long oldValue = _currentPayloadBytes;
                            _currentPayloadBytes = Math.Max(MinimumPayloadBytes, _bestPayloadBytes);
                            _payloadRollbackCount++;
                            _cooldownWindows = _common.CooldownWindows;
                            _noImprovementWindows = 0;
                            LogAdjustment("stable", "payload bytes", oldValue, _currentPayloadBytes,
                                BuildWindowReason(requestCount, windowBytes, seconds, throughput, improvement, "consecutive windows without improvement"));
                        }
                        else
                        {
                            LogWindow("stable", requestCount, windowBytes, seconds, throughput, improvement, "payload held");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ResetToInitial("upload controller failure", ex);
            }
        }

        public void RecordOversizedItem(long bytes, long itemCount)
        {
            lock (_syncRoot)
            {
                _oversizedItemCount++;
            }
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} oversized single item. Payload bytes:{bytes}, item count:{itemCount}, target bytes:{CurrentPayloadBytes}.");
        }

        public void RecordPreparation(TimeSpan elapsed) { Interlocked.Add(ref _preparationTicks, elapsed.Ticks); }
        public void RecordSerialization(TimeSpan elapsed) { Interlocked.Add(ref _serializationTicks, elapsed.Ticks); }
        public void RecordQueueWait(TimeSpan elapsed) { Interlocked.Add(ref _queueWaitTicks, elapsed.Ticks); }
        public void RecordQueueOccupancy(int count) { Interlocked.Exchange(ref _lastQueueOccupancy, count); }

        public void RecordInternalFailure(string reason)
        {
            lock (_syncRoot)
            {
                _transportOrInternalFailure = true;
            }
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} marked unsuccessful for learning. Reason:{reason}.");
        }

        public void Complete(bool succeeded)
        {
            lock (_syncRoot)
            {
                _completedSuccessfully = succeeded && !_transportOrInternalFailure;
                if (_completedSuccessfully && !_lastAdjustmentWasProtection)
                {
                    _bestReadBatch = Math.Max(1, _bestReadBatch);
                    _bestPayloadBytes = Math.Max(MinimumPayloadBytes, _bestPayloadBytes);
                }
                SyncTaskReportService.Instance.LogSummary(
                    $"Adaptive sync {_dataType} completed. Success:{_completedSuccessfully}, read configured/learned/initial/min/max/final/best:{_configuredReadInitial}/{_learned?.BestStableReadBatch ?? 0}/{_actualInitialReadBatch}/{_minimumReadBatch}/{_maximumReadBatch}/{_currentReadBatch}/{_bestReadBatch}, payload configured/learned/initial/min/max/final/best:{_configuredPayloadInitial}/{_learned?.BestStablePayloadBytes ?? 0}/{_actualInitialPayloadBytes}/{_minimumPayloadBytes}/{_maximumObservedPayloadBytes}/{_currentPayloadBytes}/{_bestPayloadBytes}, next read:{Math.Max(1, (int)Math.Round(_bestReadBatch * 0.75))}, next payload:{_bestPayloadBytes}, concurrency configured/minimum:{_settings.Upload.Concurrency}/{_minimumConcurrency}, throughput initial/best:{_baselineThroughput:F1}/{_bestThroughput:F1} bytes/s, valid windows:{_validWindows}, payload growth/rollback:{_payloadGrowthCount}/{_payloadRollbackCount}, read growth/rollback:{_readGrowthCount}/{_readRollbackCount}, HTTP protections:{_httpProtectionCount}, concurrency reductions:{_concurrencyReductionCount}, oversized items:{_oversizedItemCount}, preparation:{TimeSpan.FromTicks(_preparationTicks).TotalSeconds:F3}s, serialization:{TimeSpan.FromTicks(_serializationTicks).TotalSeconds:F3}s, queue wait:{TimeSpan.FromTicks(_queueWaitTicks).TotalSeconds:F3}s.");
            }
        }

        internal AdaptiveDataTypeState CreateLearnedState(string environmentId)
        {
            lock (_syncRoot)
            {
                if (!_completedSuccessfully || _validWindows < 2 || _bestPayloadBytes <= 0 || _lastAdjustmentWasProtection)
                {
                    return null;
                }
                return new AdaptiveDataTypeState
                {
                    BestStablePayloadBytes = _bestPayloadBytes,
                    BestStableReadBatch = _bestReadBatch,
                    BestStableThroughputBytesPerSecond = _bestThroughput,
                    ValidEvaluationWindows = _validWindows,
                    UpdatedAtUtc = DateTime.UtcNow,
                    EnvironmentId = environmentId
                };
            }
        }

        private void GrowPayload(int percent, string phase, int requests, long bytes, double seconds, double throughput, double improvement)
        {
            long oldValue = _currentPayloadBytes;
            _currentPayloadBytes = Math.Min(_maximumPayloadBytes,
                Math.Max(oldValue + 1, (long)Math.Ceiling(oldValue * (1 + percent / 100.0))));
            _lastAdjustmentWasProtection = false;
            if (_currentPayloadBytes != oldValue)
            {
                _payloadGrowthCount++;
                _maximumObservedPayloadBytes = Math.Max(_maximumObservedPayloadBytes, _currentPayloadBytes);
                LogAdjustment(phase, "payload bytes", oldValue, _currentPayloadBytes,
                    BuildWindowReason(requests, bytes, seconds, throughput, improvement, "successful upload window"));
            }
        }

        private void ApplyProtection(string signal)
        {
            long oldPayload = _currentPayloadBytes;
            _currentPayloadBytes = Math.Max(MinimumPayloadBytes, _currentPayloadBytes / 2);
            _httpProtectionCount++;
            _cooldownWindows = _common.CooldownWindows;
            _lastAdjustmentWasProtection = true;
            _windowRequestCount = 0;
            _windowPayloadBytes = 0;
            LogAdjustment("protect", "payload bytes", oldPayload, _currentPayloadBytes, "HTTP signal:" + signal);
            if (_httpProtectionCount > 1 && _effectiveConcurrency > 1)
            {
                int oldConcurrency = _effectiveConcurrency;
                _effectiveConcurrency = Math.Max(1, _effectiveConcurrency / 2);
                _minimumConcurrency = Math.Min(_minimumConcurrency, _effectiveConcurrency);
                _concurrencyReductionCount++;
                LogAdjustment("protect", "effective concurrency", oldConcurrency, _effectiveConcurrency, "persistent HTTP protection signal:" + signal);
            }
            _minimumPayloadBytes = Math.Min(_minimumPayloadBytes, _currentPayloadBytes);
        }

        private void ExitUpload()
        {
            lock (_syncRoot)
            {
                _activeUploads = Math.Max(0, _activeUploads - 1);
            }
        }

        private void ResetToInitial(string reason, Exception ex)
        {
            lock (_syncRoot)
            {
                _currentReadBatch = _configuredReadInitial;
                _currentPayloadBytes = _configuredPayloadInitial;
                _effectiveConcurrency = _settings.Upload.Concurrency;
            }
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} controller reset to initial values. Reason:{reason}. {ExceptionLogHelper.Format(ex)}");
        }

        private void LogWindow(string phase, int requests, long bytes, double seconds, double throughput, double improvement, string reason)
        {
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} {phase} window. Requests:{requests}, payload bytes:{bytes}, wall elapsed:{seconds:F3}s, throughput:{throughput:F1} bytes/s, baseline change:{improvement:F1}%, reason:{reason}.");
        }

        private void LogAdjustment(string phase, string parameter, long oldValue, long newValue, string reason)
        {
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} adjustment. Phase:{phase}, parameter:{parameter}, old:{oldValue}, new:{newValue}, queue occupancy:{Interlocked.CompareExchange(ref _lastQueueOccupancy, 0, 0)}/{CurrentQueueCapacity}, reason:{reason}.");
        }

        private static string BuildWindowReason(int requests, long bytes, double seconds, double throughput, double improvement, string reason)
        {
            return $"{reason}, requests:{requests}, payload bytes:{bytes}, wall elapsed:{seconds:F3}s, throughput:{throughput:F1} bytes/s, baseline change:{improvement:F1}%";
        }

        private static long ToBytes(double mib) { return Math.Max(MinimumPayloadBytes, (long)(mib * OneMiB)); }
        private static int Clamp(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
        private static long Clamp(long value, long min, long max) { return Math.Max(min, Math.Min(max, value)); }

        private sealed class UploadLease : IDisposable
        {
            private AdaptiveSyncController _owner;
            public UploadLease(AdaptiveSyncController owner) { _owner = owner; }
            public void Dispose() { Interlocked.Exchange(ref _owner, null)?.ExitUpload(); }
        }
    }

    public static class AdaptiveSyncService
    {
        private const string SettingsFileName = "AdaptiveSyncSettings.json";
        private const string StateFileName = "AdaptiveSyncState.json";
        private const long OneMiB = 1024L * 1024L;
        private static readonly object SyncRoot = new object();

        public static AdaptiveSyncSession Current { get; private set; }

        public static AdaptiveSyncSession BeginTask(string environmentId)
        {
            lock (SyncRoot)
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                try
                {
                    AdaptiveSyncSettings settings = LoadSettings(settingsPath, out bool settingsLoaded);
                    AdaptiveSyncState state = LoadLatestState(out string statePath);
                    Current = new AdaptiveSyncSession(settings, state, environmentId, settingsPath, settingsLoaded, statePath);
                    SyncTaskReportService.Instance.LogSummary($"Adaptive sync configuration. Expected path:{settingsPath}, source:{(settingsLoaded ? "JSON" : "built-in defaults")}, state loaded path:{statePath ?? "none"}.");
                }
                catch (Exception ex)
                {
                    SyncTaskReportService.Instance.LogSummary("Adaptive sync initialization failed; built-in defaults will be used without interrupting synchronization. " + ExceptionLogHelper.Format(ex));
                    Current = new AdaptiveSyncSession(
                        new AdaptiveSyncSettings(),
                        new AdaptiveSyncState(),
                        environmentId,
                        settingsPath,
                        false,
                        null);
                }
                return Current;
            }
        }

        public static AdaptiveSyncSession GetOrCreateCurrent()
        {
            lock (SyncRoot)
            {
                return Current ?? BeginTask(string.Empty);
            }
        }

        internal static bool TrySaveState(AdaptiveSyncState state, out string savedPath)
        {
            savedPath = null;
            string json = JsonConvert.SerializeObject(state, Formatting.Indented);
            foreach (string path in GetStatePaths())
            {
                try
                {
                    string directory = Path.GetDirectoryName(path);
                    Directory.CreateDirectory(directory);
                    string temporaryPath = path + ".tmp." + Guid.NewGuid().ToString("N");
                    File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
                    if (File.Exists(path))
                    {
                        string backupPath = path + ".bak";
                        File.Replace(temporaryPath, path, backupPath, true);
                        TryDelete(backupPath);
                    }
                    else
                    {
                        File.Move(temporaryPath, path);
                    }
                    savedPath = path;
                    return true;
                }
                catch (Exception ex)
                {
                    SyncTaskReportService.Instance.LogSummary($"Failed to save adaptive sync state to {path}. {ExceptionLogHelper.Format(ex)}");
                }
            }
            return false;
        }

        private static AdaptiveSyncSettings LoadSettings(string path, out bool loaded)
        {
            loaded = false;
            var settings = new AdaptiveSyncSettings();
            if (File.Exists(path))
            {
                try
                {
                    JsonConvert.PopulateObject(File.ReadAllText(path, Encoding.UTF8), settings);
                    loaded = true;
                }
                catch (Exception ex)
                {
                    settings = new AdaptiveSyncSettings();
                    SyncTaskReportService.Instance.LogSummary($"Failed to load adaptive sync settings from {path}; built-in defaults will be used. {ExceptionLogHelper.Format(ex)}");
                }
            }
            Normalize(settings);
            return settings;
        }

        private static AdaptiveSyncState LoadLatestState(out string loadedPath)
        {
            loadedPath = null;
            AdaptiveSyncState latest = null;
            foreach (string path in GetStatePaths())
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                try
                {
                    AdaptiveSyncState candidate = JsonConvert.DeserializeObject<AdaptiveSyncState>(File.ReadAllText(path, Encoding.UTF8));
                    if (candidate?.DataTypes == null)
                    {
                        throw new JsonException("State has no dataTypes object.");
                    }
                    candidate.DataTypes = new Dictionary<string, AdaptiveDataTypeState>(candidate.DataTypes, StringComparer.OrdinalIgnoreCase);
                    if (latest == null || candidate.UpdatedAtUtc > latest.UpdatedAtUtc)
                    {
                        latest = candidate;
                        loadedPath = path;
                    }
                }
                catch (Exception ex)
                {
                    SyncTaskReportService.Instance.LogSummary($"Failed to load adaptive sync state from {path}. {ExceptionLogHelper.Format(ex)}");
                }
            }
            return latest ?? new AdaptiveSyncState();
        }

        private static IEnumerable<string> GetStatePaths()
        {
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StateFileName);
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KindomDataAPIServer", "Configs", StateFileName);
            yield return Path.Combine(Path.GetTempPath(), "KindomDataAPIServer", "Configs", StateFileName);
        }

        private static void Normalize(AdaptiveSyncSettings settings)
        {
            settings.Common = settings.Common ?? new AdaptiveCommonSettings();
            settings.Fixed = settings.Fixed ?? new AdaptiveFixedSettings();
            settings.DataTypes = settings.DataTypes ?? new AdaptiveDataTypesSettings();
            settings.DataTypes.Formation = settings.DataTypes.Formation ?? AdaptiveDataTypeSettings.CreateFormation();
            settings.DataTypes.Trajectory = settings.DataTypes.Trajectory ?? AdaptiveDataTypeSettings.CreateTrajectory();
            settings.DataTypes.Production = settings.DataTypes.Production ?? AdaptiveDataTypeSettings.CreateProduction();
            settings.DataTypes.WellLog = settings.DataTypes.WellLog ?? AdaptiveDataTypeSettings.CreateWellLog();
            settings.Version = ClampAndLog("version", settings.Version, 1, 1);
            settings.Common.FastPayloadGrowthPercent = ClampAndLog("common.fastPayloadGrowthPercent", settings.Common.FastPayloadGrowthPercent, 1, 100);
            settings.Common.StablePayloadGrowthPercent = ClampAndLog("common.stablePayloadGrowthPercent", settings.Common.StablePayloadGrowthPercent, 1, 100);
            settings.Common.ThroughputImprovementPercent = ClampAndLog("common.throughputImprovementPercent", settings.Common.ThroughputImprovementPercent, 0, 100);
            settings.Common.RollbackAfterNoImprovementWindows = ClampAndLog("common.rollbackAfterNoImprovementWindows", settings.Common.RollbackAfterNoImprovementWindows, 1, 20);
            settings.Common.CooldownWindows = ClampAndLog("common.cooldownWindows", settings.Common.CooldownWindows, 0, 20);
            settings.Fixed.WellHeaderUploadBatchSize = ClampAndLog("fixed.wellHeaderUploadBatchSize", settings.Fixed.WellHeaderUploadBatchSize, 1, 50000);
            NormalizeType("formation", settings.DataTypes.Formation, 1000, 16, 2000000);
            NormalizeType("trajectory", settings.DataTypes.Trajectory, 1000, 16, 5000000);
            NormalizeType("production", settings.DataTypes.Production, 500, 16, 1000000);
            NormalizeType("wellLog", settings.DataTypes.WellLog, 200, 16, 10000000);
        }

        private static void NormalizeType(string name, AdaptiveDataTypeSettings settings, int absoluteReadMax, int absoluteConcurrencyMax, int absoluteBusinessMax)
        {
            settings.ReadBatch = settings.ReadBatch ?? new AdaptiveReadBatchSettings();
            settings.Upload = settings.Upload ?? new AdaptiveUploadSettings();
            settings.ReadBatch.Max = ClampAndLog($"dataTypes.{name}.readBatch.max", settings.ReadBatch.Max, 1, absoluteReadMax);
            settings.ReadBatch.Initial = ClampAndLog($"dataTypes.{name}.readBatch.initial", settings.ReadBatch.Initial, 1, settings.ReadBatch.Max);
            settings.ReadBatch.TargetSeconds = ClampAndLog($"dataTypes.{name}.readBatch.targetSeconds", settings.ReadBatch.TargetSeconds, 0.1, 600);
            settings.Upload.MaxPayloadMiB = ClampAndLog($"dataTypes.{name}.upload.maxPayloadMiB", settings.Upload.MaxPayloadMiB, 0.0625, 64);
            settings.Upload.InitialPayloadMiB = ClampAndLog($"dataTypes.{name}.upload.initialPayloadMiB", settings.Upload.InitialPayloadMiB, 0.0625, settings.Upload.MaxPayloadMiB);
            settings.Upload.Concurrency = ClampAndLog($"dataTypes.{name}.upload.concurrency", settings.Upload.Concurrency, 1, absoluteConcurrencyMax);
            settings.Upload.StableEvaluationRequests = ClampAndLog($"dataTypes.{name}.upload.stableEvaluationRequests", settings.Upload.StableEvaluationRequests, 1, 100);
            settings.Upload.MaxFormationCount = ClampAndLog($"dataTypes.{name}.upload.maxFormationCount", settings.Upload.MaxFormationCount, 1, absoluteBusinessMax);
            settings.Upload.MaxPointCount = ClampAndLog($"dataTypes.{name}.upload.maxPointCount", settings.Upload.MaxPointCount, 1, absoluteBusinessMax);
            settings.Upload.MaxDailyDataCount = ClampAndLog($"dataTypes.{name}.upload.maxDailyDataCount", settings.Upload.MaxDailyDataCount, 1, absoluteBusinessMax);
            settings.Upload.MaxSampleCount = ClampAndLog($"dataTypes.{name}.upload.maxSampleCount", settings.Upload.MaxSampleCount, 1, absoluteBusinessMax);
        }

        private static int ClampAndLog(string name, int value, int min, int max)
        {
            int normalized = Math.Max(min, Math.Min(max, value));
            if (normalized != value) SyncTaskReportService.Instance.LogSummary($"Adaptive sync setting normalized. Name:{name}, configured:{value}, effective:{normalized}.");
            return normalized;
        }

        private static double ClampAndLog(string name, double value, double min, double max)
        {
            double normalized = double.IsNaN(value) || double.IsInfinity(value) ? min : Math.Max(min, Math.Min(max, value));
            if (Math.Abs(normalized - value) > double.Epsilon) SyncTaskReportService.Instance.LogSummary($"Adaptive sync setting normalized. Name:{name}, configured:{value}, effective:{normalized}.");
            return normalized;
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
